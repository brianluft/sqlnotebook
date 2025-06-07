using System;
using System.Data.Common;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;

namespace SqlNotebook.Import.Database;

public partial class DatabaseConnectionForm : ZForm
{
    private readonly DbConnectionStringBuilder _builder;
    private readonly IImportSession _session;

    public sealed class BasicOptions
    {
        public string Server { get; set; } = "";
        public string Database { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool UseWindowsAuth { get; set; } = false;
    }

    public DatabaseConnectionForm(string title, DbConnectionStringBuilder builder, IImportSession session)
    {
        InitializeComponent();

        _builder = builder;
        _session = session;

        Ui ui = new(this, 75, 30);
        ui.Init(_table);
        ui.Init(_tabs);
        ui.Init(_basicTable);
        ui.Pad(_basicTable);
        ui.Init(_addressLabel);
        ui.Init(_serverTxt);
        ui.Init(_databaseLabel);
        ui.MarginTop(_databaseLabel);
        ui.Init(_databaseTxt);
        ui.Init(_usernameLabel);
        ui.MarginTop(_usernameLabel);
        ui.Init(_usernameTxt);
        ui.Init(_passwordLabel);
        ui.MarginTop(_passwordLabel);
        ui.Init(_passwordTxt);
        ui.Init(_windowsAuthChk);
        ui.MarginTop(_windowsAuthChk);
        ui.Init(_propertyGrid);
        ui.Init(_buttonFlow1);
        ui.MarginTop(_buttonFlow1);
        ui.Init(_clearButton);
        ui.Init(_buttonFlow2);
        ui.MarginTop(_buttonFlow2);
        ui.Init(_okBtn);
        ui.Init(_cancelBtn);

        if (builder is not SqlConnectionStringBuilder)
        {
            _windowsAuthChk.Visible = false;
        }
        if (builder is MySqlConnectionStringBuilder)
        {
            _databaseLabel.Text = "&Schema name:";
        }
        _propertyGrid.SelectedObject = builder;
        Text = title;

        UpdateBasicOptionsUi(_session.GetBasicOptions(builder)); // populate basic options with info from builder

        Load += delegate
        {
            _serverTxt.Select();
        };
    }

    private void UpdateBasicOptionsUi(BasicOptions opt)
    {
        _serverTxt.Text = opt.Server;
        _databaseTxt.Text = opt.Database;
        _usernameTxt.Text = opt.Username;
        _passwordTxt.Text = opt.Password;
        _windowsAuthChk.Checked = opt.UseWindowsAuth;
    }

    private BasicOptions ReadBasicOptionsUi()
    {
        return new BasicOptions
        {
            Server = _serverTxt.Text,
            Database = _databaseTxt.Text,
            Username = _usernameTxt.Text,
            Password = _passwordTxt.Text,
            UseWindowsAuth = _windowsAuthChk.Checked,
        };
    }

    private void OkBtn_Click(object sender, EventArgs e)
    {
        if (_tabs.SelectedIndex == 0)
        {
            // "Basic" tab is selected.
            _session.SetBasicOptions(_builder, ReadBasicOptionsUi());
        }
        DialogResult = DialogResult.OK;
        Close();
    }

    private void WindowsAuthChk_CheckedChanged(object sender, EventArgs e)
    {
        _usernameTxt.Enabled = _passwordTxt.Enabled = !_windowsAuthChk.Checked;
    }

    private void Tabs_TabIndexChanged(object sender, EventArgs e)
    {
        if (_tabs.SelectedIndex == 0)
        {
            // "Basic" tab is now selected.
            UpdateBasicOptionsUi(_session.GetBasicOptions(_builder));
        }
        else
        {
            // "Advanced" tab is now selected.
            _session.SetBasicOptions(_builder, ReadBasicOptionsUi());
            _propertyGrid.SelectedObject = _builder; // Refresh the property grid.
        }
    }

    private void ClearButton_Click(object sender, EventArgs e)
    {
        _session.Clear(_builder);
        UpdateBasicOptionsUi(_session.GetBasicOptions(_builder)); // populate basic options with info from builder
    }
}
