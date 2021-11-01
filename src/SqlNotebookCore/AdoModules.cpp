#include <msclr/auto_handle.h>
#include "gcroot.h"
#include "SqlNotebookCore.h"
using namespace SqlNotebookCore;
using namespace System::Data;
using namespace System::Data::SqlClient;
using namespace System::IO;
using namespace System::Linq;
using namespace System::Text;
using namespace Npgsql;
using namespace msclr;
using namespace MySql::Data::MySqlClient;

// these Object^s should be String^ but I ran into strange linker errors
private struct AdoCreateInfo {
    gcroot<Func<Object^, IDbConnection^>^> ConnectionCreator;
    gcroot<Object^> SelectRandomSampleSql; // should contain {0} as a placeholder for the escaped table name including surrounding quotes
    gcroot<Object^> SelectRandomSampleSqlFallback; // for SQL Server we need to fall back to a non-TABLESAMPLE for views
};

private struct AdoTable {
    sqlite3_vtab Super;
    gcroot<Object^> ConnectionString;
    gcroot<Object^> AdoTableName;
    gcroot<Object^> AdoSchemaName;
    gcroot<List<Object^>^> ColumnNames;
    gcroot<Func<Object^, IDbConnection^>^> ConnectionCreator;
    int64_t InitialRowCount;

    // column name => estimated rows as a fraction (0-1) of the total row count
    gcroot<Dictionary<Object^, double>^> EstimatedRowsPercentByColumn;

    AdoTable() {
        memset(&Super, 0, sizeof(Super));
    }
};

private struct AdoCursor {
    sqlite3_vtab_cursor Super;
    AdoTable* Table;
    gcroot<IDbConnection^> Connection;
    gcroot<IDbCommand^> Command;
    gcroot<IDataReader^> Reader;
    gcroot<Object^> ReaderSql;
    bool IsEof;

    AdoCursor() {
        memset(&Super, 0, sizeof(Super));
        Table = nullptr;
    }
};

static int AdoCreate(sqlite3* db, void* pAux, int argc, const char* const* argv, sqlite3_vtab** ppVTab, char** pzErr) {
    // argv[3]: connectionString
    // argv[4]: table name
#ifdef _DEBUG
    System::Diagnostics::Debug::WriteLine("AdoCreate");
#endif

    AdoTable* vtab = nullptr;
    auto createInfo = (AdoCreateInfo*)pAux;

    try {
        if (argc != 5 && argc != 6) {
            throw gcnew Exception("Syntax: CREATE VIRTUAL TABLE <name> USING <driver> ('<connection string>', 'table name', ['schema name']);");
        }
        auto connStr = SqlNotebookCore::Util::Str(argv[3])->Trim(L'\'');
        auto adoTableName = SqlNotebookCore::Util::Str(argv[4])->Trim(L'\'');
        auto adoSchemaName = SqlNotebookCore::Util::Str(argc == 6 ? argv[5] : "")->Trim(L'\'');
        auto adoQuotedCombinedName = adoSchemaName->Length > 0
            ? String::Format("\"{0}\".\"{1}\"", adoSchemaName->Replace("\"", "\"\""), adoTableName->Replace("\"", "\"\"")) 
            : String::Format("\"{0}\"", adoTableName->Replace("\"", "\"\""));
        auto_handle<IDbConnection> conn(createInfo->ConnectionCreator->Invoke(connStr));
        conn->Open();

        // ensure the table exists and detect the column names
        auto columnNames = gcnew List<Object^>();
        auto columnTypes = gcnew List<Type^>();
        {
            auto_handle<IDbCommand> cmd(conn->CreateCommand());
            cmd->CommandText = "SELECT * FROM " + adoQuotedCombinedName + " WHERE 1 = 0";
            auto_handle<IDataReader> reader(cmd->ExecuteReader());
            for (int i = 0; i < reader->FieldCount; i++) {
                columnNames->Add(reader->GetName(i));
                columnTypes->Add(reader->GetFieldType(i));
            }
        }

        // create sqlite structure
        vtab = new AdoTable;
        vtab->ConnectionString = connStr;
        vtab->AdoTableName = adoTableName;
        vtab->AdoSchemaName = adoSchemaName;
        vtab->ColumnNames = columnNames;
        vtab->ConnectionCreator = createInfo->ConnectionCreator;

        // get the row count
        {
            auto_handle<IDbCommand> cmd(conn->CreateCommand());
            cmd->CommandText = "SELECT COUNT(*) FROM " + adoQuotedCombinedName;
            vtab->InitialRowCount = Convert::ToInt64(cmd->ExecuteScalar());
        }

        // take a random sample of rows and compute some basic statistics
        {
            auto_handle<IDataReader> reader{};
            try {
                auto_handle<IDbCommand> cmd(conn->CreateCommand());
                cmd->CommandText = ((String^)(Object^)createInfo->SelectRandomSampleSql)->Replace("{0}", adoQuotedCombinedName);
                reader = auto_handle<IDataReader>{ cmd->ExecuteReader() };
            } catch (Exception^ ex) {
                // On MSSQL we can't use TABLESAMPLE for views, so detect that error and run the fallback instead.
                if (ex->Message->Contains("TABLESAMPLE")) {
                    auto_handle<IDbCommand> cmd(conn->CreateCommand());
                    cmd->CommandText = ((String^)(Object^)createInfo->SelectRandomSampleSqlFallback)->Replace("{0}", adoQuotedCombinedName);
                    reader = auto_handle<IDataReader>{ cmd->ExecuteReader() };
                } else {
                    throw;
                }
            }

            auto colCount = columnNames->Count;

            // hash code => number of appearances of that hash in the column
            auto colDicts = gcnew array<Dictionary<int, int>^>(colCount);

            for (int i = 0; i < colCount; i++) {
                colDicts[i] = gcnew Dictionary<int, int>();
            }
            auto row = gcnew array<Object^>(colCount);
            int sampleSize = 0;
            while (reader->Read()) {
                sampleSize++;
                reader->GetValues(row);
                for (int i = 0; i < colCount; i++) {
                    auto colDict = colDicts[i];
                    int hash = row[i] == nullptr ? 0 : row[i]->GetHashCode();
                    int count;
                    if (colDict->TryGetValue(hash, count)) {
                        colDict[hash] = count + 1;
                    } else {
                        colDict[hash] = 1;
                    }
                }
            }
            // this is the average number of rows we expect any arbitrary value to appear in the column
            // for instance, if the column is a list of 500 coin flips 0 or 1, an average around 250 is expected
            vtab->EstimatedRowsPercentByColumn = gcnew Dictionary<Object^, double>(colCount);
            for (int i = 0; i < colCount; i++) {
                auto name = columnNames[i];
                auto value = sampleSize > 0 ? (Enumerable::Average(colDicts[i]->Values) / sampleSize) : 1.0;
                vtab->EstimatedRowsPercentByColumn->default[name] = value;
#ifdef _DEBUG
                System::Diagnostics::Debug::WriteLine("   Column " + name + " estimated rows per unique value = " + value.ToString());
#endif
            }
        }

        // register the column names and types with pgsql
        auto columnLines = gcnew List<String^>();
        for (int i = 0; i < columnNames->Count; i++) {
            auto t = columnTypes[i];
            String^ sqlType;
            if (t == Int16::typeid || t == Int32::typeid || t == Int64::typeid || t == Byte::typeid || t == Boolean::typeid) {
                sqlType = "integer";
            } else if (t == Single::typeid || t == Double::typeid || t == Decimal::typeid) {
                sqlType = "real";
            } else {
                sqlType = "text";
            }
            columnLines->Add("\"" + ((String^)columnNames[i])->Replace("\"", "\"\"") + "\" " + sqlType);
        }
        auto createSql = "CREATE TABLE a (" + String::Join(", ", columnLines) + ")";
        g_SqliteCall(db, sqlite3_declare_vtab(db, SqlNotebookCore::Util::CStr(createSql).c_str()));

        *ppVTab = &vtab->Super;
        return SQLITE_OK;
    } catch (Exception^ ex) {
        delete vtab;
        *pzErr = sqlite3_mprintf("AdoCreate: %s", SqlNotebookCore::Util::CStr(ex->Message).c_str());
        return SQLITE_ERROR;
    }
}

static int AdoDestroy(sqlite3_vtab* pVTab) {
#ifdef _DEBUG
    System::Diagnostics::Debug::WriteLine("AdoDestroy");
#endif
    delete (AdoTable*)pVTab;
    return SQLITE_OK;
}

static int AdoBestIndex(sqlite3_vtab* pVTab, sqlite3_index_info* info) {
    auto vtab = (AdoTable*)pVTab;
#ifdef _DEBUG
    System::Diagnostics::Debug::WriteLine("AdoBestIndex - " + vtab->AdoTableName);
#endif

    // build a query corresponding to the request
    auto sb = gcnew StringBuilder();
    sb->Append("SELECT * FROM ");
    auto schemaName = (String^)(Object^)vtab->AdoSchemaName;
    auto tableName = (String^)(Object^)vtab->AdoTableName;
    auto adoQuotedCombinedName = schemaName->Length > 0
        ? String::Format("\"{0}\".\"{1}\"", schemaName->Replace("\"", "\"\""), tableName->Replace("\"", "\"\""))
        : String::Format("\"{0}\"", tableName->Replace("\"", "\"\""));
    sb->Append(adoQuotedCombinedName);

    // where clause
    int argvIndex = 1;
    double estimatedRowsPercent = 1;
    if (info->nConstraint > 0) {
        auto terms = gcnew List<String^>();
        for (int i = 0; i < info->nConstraint; i++) {
            if (info->aConstraint[i].iColumn == -1) {
                continue; // rowid instead of a column. we don't support this type of constraint.
            } else if (!info->aConstraint[i].usable) {
                continue;
            }

            String^ op;
            switch (info->aConstraint[i].op) {
                case SQLITE_INDEX_CONSTRAINT_EQ: op = " = "; break;
                case SQLITE_INDEX_CONSTRAINT_GT: op = " > "; break;
                case SQLITE_INDEX_CONSTRAINT_LE: op = " <= "; break;
                case SQLITE_INDEX_CONSTRAINT_LT: op = " < "; break;
                case SQLITE_INDEX_CONSTRAINT_GE: op = " >= "; break;
                case SQLITE_INDEX_CONSTRAINT_LIKE: op = " LIKE "; break;
                default: continue; // we don't support this operator
            }

            info->aConstraintUsage[i].argvIndex = argvIndex;
            info->aConstraintUsage[i].omit = true;
            auto columnName = (String^)vtab->ColumnNames->default[info->aConstraint[i].iColumn];
            terms->Add("\"" + columnName->Replace("\"", "\"\"") + "\"" + op + "@arg" + argvIndex);
            argvIndex++;

            estimatedRowsPercent *= vtab->EstimatedRowsPercentByColumn->default[columnName];
        }
        if (terms->Count > 0) {
            sb->Append(" WHERE ");
            sb->Append(String::Join(" AND ", terms));
        }
    }

    // order by clause
    if (info->nOrderBy > 0) {
        sb->Append(" ORDER BY ");
        auto terms = gcnew List<String^>();
        for (int i = 0; i < info->nOrderBy; i++) {
            terms->Add((String^)vtab->ColumnNames->default[info->aOrderBy[i].iColumn] + (info->aOrderBy[i].desc ? " DESC" : ""));
        }
        sb->Append(String::Join(", ", terms));
        info->orderByConsumed = true;
    }

#ifdef _DEBUG
    System::Diagnostics::Debug::WriteLine(sb);
#endif
    auto sql = SqlNotebookCore::Util::CStr(sb->ToString());
    info->idxNum = 0;
    info->idxStr = sqlite3_mprintf("%s", sql.c_str());
    info->needToFreeIdxStr = true;
    info->estimatedRows = Math::Max(int64_t(1), int64_t(estimatedRowsPercent * vtab->InitialRowCount));
    info->estimatedCost = (double)info->estimatedRows + 10000000;
        // the large constant is to make sure remote queries are always considered vastly more expensive than local
        // queries, while not affecting the relative cost of different remote query plans

    return SQLITE_OK;
}

static int AdoOpen(sqlite3_vtab* pVTab, sqlite3_vtab_cursor** ppCursor) {
#ifdef _DEBUG
    System::Diagnostics::Debug::WriteLine("AdoOpen");
#endif
    auto cursor = new AdoCursor;
    cursor->Table = (AdoTable*)pVTab;
    cursor->Connection = cursor->Table->ConnectionCreator->Invoke(cursor->Table->ConnectionString);
    cursor->Connection->Open();
    *ppCursor = &cursor->Super;
    return SQLITE_OK;
}

static int AdoClose(sqlite3_vtab_cursor* pCur) {
#ifdef _DEBUG
    System::Diagnostics::Debug::WriteLine("AdoClose");
#endif
    auto cursor = (AdoCursor*)pCur;
    
    IDataReader^ reader = cursor->Reader;
    delete reader;
    cursor->Reader = nullptr;

    IDbCommand^ command = cursor->Command;
    delete command;
    cursor->Command = nullptr;

    IDbConnection^ conn = cursor->Connection;
    delete conn;
    cursor->Connection = nullptr;
    
    delete cursor;
    return SQLITE_OK;
}

static int AdoFilter(sqlite3_vtab_cursor* pCur, int idxNum, const char* idxStr, int argc, sqlite3_value** argv) {
#ifdef _DEBUG
    System::Diagnostics::Debug::WriteLine("AdoFilter: " + SqlNotebookCore::Util::Str(idxStr));
#endif
    try {
        auto cursor = (AdoCursor*)pCur;
        auto sql = SqlNotebookCore::Util::Str(idxStr);
        auto args = gcnew array<Object^>(argc);

        for (int i = 0; i < argc; i++) {
            switch (sqlite3_value_type(argv[i])) {
                case SQLITE_INTEGER:
                    args[i] = sqlite3_value_int64(argv[i]);
                    break;
                case SQLITE_FLOAT:
                    args[i] = sqlite3_value_double(argv[i]);
                    break;
                case SQLITE_NULL:
                    args[i] = DBNull::Value;
                    break;
                case SQLITE_TEXT:
                    args[i] = SqlNotebookCore::Util::Str((const wchar_t*)sqlite3_value_text16(argv[i]));
                    break;
                default:
                    throw gcnew Exception("Data type not supported.");
            }
        }

        delete cursor->Reader;
        cursor->Reader = nullptr;

        if ((Object^)cursor->ReaderSql != nullptr && sql == (String^)(Object^)cursor->ReaderSql) {
            // sqlite is issuing new arguments for the same statement
            for (int i = 0; i < argc; i++) {
                auto parameter = (IDataParameter^)cursor->Command->Parameters[i];
                parameter->Value = args[i];
#ifdef _DEBUG
                System::Diagnostics::Debug::WriteLine("   Change: " + parameter->ParameterName + " = " + args[i]->ToString());
#endif
            }
        } else {
            // brand new statement
            delete cursor->Command;
            cursor->Command = cursor->Connection->CreateCommand();
            cursor->Command->CommandText = sql;

            for (int i = 0; i < argc; i++) {
                auto varName = "@arg" + (i + 1).ToString();
                auto parameter = cursor->Command->CreateParameter();
                parameter->ParameterName = varName;
                parameter->Value = args[i];
                cursor->Command->Parameters->Add(parameter);
#ifdef _DEBUG
                System::Diagnostics::Debug::WriteLine("   " + varName + " = " + args[i]->ToString());
#endif
            }
        }

        cursor->Reader = cursor->Command->ExecuteReader();
        cursor->ReaderSql = sql;
        cursor->IsEof = !cursor->Reader->Read();
        return SQLITE_OK;
    } catch (Exception^) {
        return SQLITE_ERROR;
    }
}

static int AdoNext(sqlite3_vtab_cursor* pCur) {
    auto cursor = (AdoCursor*)pCur;
    cursor->IsEof = !cursor->Reader->Read();
    return SQLITE_OK;
}

static int AdoEof(sqlite3_vtab_cursor* pCur) {
    auto cursor = (AdoCursor*)pCur;
    return cursor->IsEof ? 1 : 0;
}

static void ResultText16(sqlite3_context* ctx, Object^ strObj) {
    auto str = (String^)strObj;
    auto wstr = SqlNotebookCore::Util::WStr(str);
    auto wstrCopy = _wcsdup(wstr.c_str());
    auto lenB = wstr.size() * sizeof(wchar_t);
    sqlite3_result_text16(ctx, wstrCopy, (int)lenB, free);
}

static int AdoColumn(sqlite3_vtab_cursor* pCur, sqlite3_context* ctx, int n) {
    try {
        auto cursor = (AdoCursor*)pCur;
        if (cursor->IsEof) {
            return SQLITE_ERROR;
        }
        auto type = cursor->Reader->GetFieldType(n);
        if (cursor->Reader->IsDBNull(n)) {
            sqlite3_result_null(ctx);
        } else if (type == Int16::typeid) {
            sqlite3_result_int(ctx, cursor->Reader->GetInt16(n));
        } else if (type == Int32::typeid) {
            sqlite3_result_int(ctx, cursor->Reader->GetInt32(n));
        } else if (type == Int64::typeid) {
            sqlite3_result_int64(ctx, cursor->Reader->GetInt64(n));
        } else if (type == Byte::typeid) {
            sqlite3_result_int(ctx, cursor->Reader->GetByte(n));
        } else if (type == Single::typeid) {
            sqlite3_result_double(ctx, cursor->Reader->GetFloat(n));
        } else if (type == Double::typeid) {
            sqlite3_result_double(ctx, cursor->Reader->GetDouble(n));
        } else if (type == Decimal::typeid) {
            sqlite3_result_double(ctx, (double)cursor->Reader->GetDecimal(n));
        } else if (type == String::typeid) {
            ResultText16(ctx, cursor->Reader->GetString(n));
        } else if (type == Char::typeid) {
            ResultText16(ctx, gcnew String(cursor->Reader->GetChar(n), 1));
        } else if (type == Boolean::typeid) {
            sqlite3_result_int(ctx, cursor->Reader->GetBoolean(n) ? 1 : 0);
        } else if (type == NpgsqlTypes::NpgsqlDate::typeid) {
            auto reader = (NpgsqlDataReader^)(IDataReader^)cursor->Reader;
            ResultText16(ctx, ((DateTime)reader->GetDate(n)).ToString("yyyy-MM-dd"));
        } else if (type == NpgsqlTypes::NpgsqlDateTime::typeid || type == DateTime::typeid) {
            ResultText16(ctx, ((DateTime)cursor->Reader->GetDateTime(n)).ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
        } else {
            ResultText16(ctx, cursor->Reader->GetValue(n)->ToString());
        }
        return SQLITE_OK;
    } catch (Exception^) {
        return SQLITE_ERROR;
    }
}

static int AdoRowid(sqlite3_vtab_cursor* pCur, sqlite3_int64* pRowid) {
    auto cursor = (AdoCursor*)pCur;
    array<Object^>^ values = gcnew array<Object^>(cursor->Table->ColumnNames->Count);
    cursor->Reader->GetValues(values);
    int64_t hash = 13;
    for each (auto value in values) {
        hash = (hash * 7) + value->GetHashCode();
    }
    *pRowid = hash;
    return SQLITE_OK;
}

static int AdoRename(sqlite3_vtab* pVtab, const char* zNew) {
    // don't care
    return SQLITE_OK;
}

static void AdoPopulateModule(sqlite3_module* module) {
    memset(module, 0, sizeof(sqlite3_module));
    module->iVersion = 1;
    module->xCreate = AdoCreate;
    module->xConnect = AdoCreate;
    module->xBestIndex = AdoBestIndex;
    module->xDisconnect = AdoDestroy;
    module->xDestroy = AdoDestroy;
    module->xOpen = AdoOpen;
    module->xClose = AdoClose;
    module->xFilter = AdoFilter;
    module->xNext = AdoNext;
    module->xEof = AdoEof;
    module->xColumn = AdoColumn;
    module->xRowid = AdoRowid;
    module->xRename = AdoRename;
}

// --- PostgreSQL ---
static sqlite3_module s_pgModule = { 0 };
static AdoCreateInfo s_pgCreateInfo;
static IDbConnection^ PgCreateConnection(Object^ connStr) {
    return gcnew NpgsqlConnection((String^)connStr);
}
void Notebook::InstallPgModule() {
    if (s_pgModule.iVersion != 1) {
        AdoPopulateModule(&s_pgModule);
        s_pgCreateInfo.ConnectionCreator = gcnew Func<Object^, IDbConnection^>(PgCreateConnection);
        s_pgCreateInfo.SelectRandomSampleSql = "SELECT * FROM {0} ORDER BY RANDOM() LIMIT 5000;";
    }
    SqliteCall(sqlite3_create_module_v2(_sqlite, "pgsql", &s_pgModule, &s_pgCreateInfo, NULL));
}

// --- Microsoft SQL Server ---
static sqlite3_module s_msModule = { 0 };
static AdoCreateInfo s_msCreateInfo;
static IDbConnection^ MsCreateConnection(Object^ connStr) {
    return gcnew SqlConnection((String^)connStr);
}
void Notebook::InstallMsModule() {
    if (s_msModule.iVersion != 1) {
        AdoPopulateModule(&s_msModule);
        s_msCreateInfo.ConnectionCreator = gcnew Func<Object^, IDbConnection^>(MsCreateConnection);
        // TABLESAMPLE doesn't work for views, and we don't have an easy way to differentiate tables and views here.
        // So we will try TABLESAMPLE and fallback to TOP 5000 if it fails.
        s_msCreateInfo.SelectRandomSampleSql = "SELECT * FROM {0} TABLESAMPLE (5000 ROWS);";
        s_msCreateInfo.SelectRandomSampleSqlFallback = "SELECT TOP 5000 * FROM {0}";
    }
    SqliteCall(sqlite3_create_module_v2(_sqlite, "mssql", &s_msModule, &s_msCreateInfo, NULL));
}

// --- MySQL ---
static sqlite3_module s_myModule = { 0 };
static AdoCreateInfo s_myCreateInfo;
static IDbConnection^ MyCreateConnection(Object^ connStr) {
    return gcnew MySqlConnection((String^)connStr);
}
void Notebook::InstallMyModule() {
    if (s_myModule.iVersion != 1) {
        AdoPopulateModule(&s_myModule);
        s_myCreateInfo.ConnectionCreator = gcnew Func<Object^, IDbConnection^>(MyCreateConnection);
        s_myCreateInfo.SelectRandomSampleSql = "SELECT * FROM {0} ORDER BY RAND() LIMIT 5000;";
    }
    SqliteCall(sqlite3_create_module_v2(_sqlite, "mysql", &s_myModule, &s_myCreateInfo, NULL));
}
