﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace CrazyKTV_SongMgr
{
    public partial class MainFrom : Form
    {
        private void SongMgrCfg_Save_Button_Click(object sender, EventArgs e)
        {
            if (Global.SongMgrSongType == "") Global.SongMgrSongType = "null";
            SongMgrCfg_GetSongMgrLangCode();
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "CrazyktvDatabaseFile", Global.CrazyktvDatabaseFile);
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrSupportFormat", Global.SongMgrSupportFormat);
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrDestFolder", Global.SongMgrDestFolder);
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrSongAddMode", Global.SongMgrSongAddMode);
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrChorusMerge", Global.SongMgrChorusMerge);
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrMaxDigitCode", Global.SongMgrMaxDigitCode);
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrLangCode", Global.SongMgrLangCode);
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrSongType", Global.SongMgrSongType);
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrSongInfoSeparate", Global.SongMgrSongInfoSeparate);
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrChorusSeparate", Global.SongMgrChorusSeparate);
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrFolderStructure", Global.SongMgrFolderStructure);
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrFileStructure", Global.SongMgrFileStructure);
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrSongTrackMode", Global.SongMgrSongTrackMode);
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrBackupRemoveSong", Global.SongMgrBackupRemoveSong);
        }

        private void SongMgrCfg_DBFile_Button_Click(object sender, EventArgs e)
        {
            OpenFileDialog opd = new OpenFileDialog();
            if (SongMgrCfg_DBFile_TextBox.Text != "") opd.InitialDirectory = Path.GetDirectoryName(SongMgrCfg_DBFile_TextBox.Text);
            opd.Filter = "資料庫檔案 (*.mdb)|*.mdb";
            opd.FilterIndex = 1;


            if (opd.ShowDialog() == DialogResult.OK && opd.FileName.Length > 0)
            {
                Global.CrazyktvDatabaseFile = opd.FileName;
                SongMgrCfg_DBFile_TextBox.Text = opd.FileName;

                // 檢查資料庫檔案是否為舊版資料庫
                Common_CheckDBVer();

                // 統計歌曲數量
                Task.Factory.StartNew(() => Common_GetSongStatisticsTask());

                // 統計歌手數量
                Task.Factory.StartNew(() => Common_GetSingerStatisticsTask());

                // 載入我的最愛清單
                Global.SongQueryFavoriteQuery = "False";
                SongQuery_GetFavoriteUserList();
                SongMaintenance_GetFavoriteUserList();

                CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "CrazyktvDatabaseFile", Global.CrazyktvDatabaseFile);
            }
        }

        private void SongMgrCfg_SupportFormat_TextBox_TextChanged(object sender, EventArgs e)
        {
            Global.SongMgrSupportFormat = SongMgrCfg_SupportFormat_TextBox.Text.ToLower();
        }

        private void SongMgrCfg_DestFolder_Button_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog opd = new FolderBrowserDialog();
            if (SongMgrCfg_DestFolder_TextBox.Text != "") opd.SelectedPath = SongMgrCfg_DestFolder_TextBox.Text;

            if (opd.ShowDialog() == DialogResult.OK && opd.SelectedPath.Length > 0)
            {
                Global.SongMgrDestFolder = opd.SelectedPath;
                SongMgrCfg_DestFolder_TextBox.Text = opd.SelectedPath;

                if (Global.CrazyktvDatabaseVer == "Error" | !File.Exists(Global.CrazyktvDatabaseFile) | !Directory.Exists(Global.SongMgrDestFolder))
                {
                    Common_SwitchDBVerErrorUI(false);
                }
                else
                {
                    // 統計歌曲數量
                    Task.Factory.StartNew(() => Common_GetSongStatisticsTask());
                    Common_SwitchDBVerErrorUI(true);
                }

                CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrDestFolder", Global.SongMgrDestFolder);
            }
        }

        private void SongMgrCfg_SongAddMode_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (SongMgrCfg_SongAddMode_ComboBox.SelectedValue.ToString())
            {
                case "1":
                case "2":
                    Global.SongMgrSongAddMode = SongMgrCfg_SongAddMode_ComboBox.SelectedValue.ToString();
                    break;
            }
        }

        private void SongMgrCfg_CrtchorusMerge_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Global.SongMgrChorusMerge = SongMgrCfg_CrtchorusMerge_CheckBox.Checked.ToString();
        }

        private void SongMgrCfg_SongTrackMode_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Global.SongMgrSongTrackMode = SongMgrCfg_SongTrackMode_CheckBox.Checked.ToString();
        }

        private void SongMgrCfg_MaxDigitCode_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (SongMgrCfg_MaxDigitCode_ComboBox.SelectedValue.ToString())
            {
                case "1":
                case "2":
                    Global.SongMgrMaxDigitCode = SongMgrCfg_MaxDigitCode_ComboBox.SelectedValue.ToString();
                    SongMgrCfg_RefreshSongMgrLangCode();
                    break;
            }
        }

        private void SongMgrCfg_RefreshSongMgrLangCode()
        {
            switch (SongMgrCfg_MaxDigitCode_ComboBox.SelectedValue.ToString())
            {
                case "1":
                    Global.SongMgrLangCode = "10000,20000,30000,40000,50000,60000,70000,80000,90000,95000";
                    break;
                case "2":
                    Global.SongMgrLangCode = "100000,200000,300000,400000,500000,600000,700000,800000,900000,950000";
                    break;
            }
            string[] str = Global.SongMgrLangCode.Split(',');
            SongMgrCfg_Lang1Code_TextBox.Text = str[0];
            SongMgrCfg_Lang2Code_TextBox.Text = str[1];
            SongMgrCfg_Lang3Code_TextBox.Text = str[2];
            SongMgrCfg_Lang4Code_TextBox.Text = str[3];
            SongMgrCfg_Lang5Code_TextBox.Text = str[4];
            SongMgrCfg_Lang6Code_TextBox.Text = str[5];
            SongMgrCfg_Lang7Code_TextBox.Text = str[6];
            SongMgrCfg_Lang8Code_TextBox.Text = str[7];
            SongMgrCfg_Lang9Code_TextBox.Text = str[8];
            SongMgrCfg_Lang10Code_TextBox.Text = str[9];
        }

        private void SongMgrCfg_LoadSongMgrLangCode()
        {
            string[] str = Global.SongMgrLangCode.Split(',');
            SongMgrCfg_Lang1Code_TextBox.Text = str[0];
            SongMgrCfg_Lang2Code_TextBox.Text = str[1];
            SongMgrCfg_Lang3Code_TextBox.Text = str[2];
            SongMgrCfg_Lang4Code_TextBox.Text = str[3];
            SongMgrCfg_Lang5Code_TextBox.Text = str[4];
            SongMgrCfg_Lang6Code_TextBox.Text = str[5];
            SongMgrCfg_Lang7Code_TextBox.Text = str[6];
            SongMgrCfg_Lang8Code_TextBox.Text = str[7];
            SongMgrCfg_Lang9Code_TextBox.Text = str[8];
            SongMgrCfg_Lang10Code_TextBox.Text = str[9];
        }

        private void SongMgrCfg_GetSongMgrLangCode()
        {
            string[] strlist = new[]
            {
                SongMgrCfg_Lang1Code_TextBox.Text,
                SongMgrCfg_Lang2Code_TextBox.Text,
                SongMgrCfg_Lang3Code_TextBox.Text,
                SongMgrCfg_Lang4Code_TextBox.Text,
                SongMgrCfg_Lang5Code_TextBox.Text,
                SongMgrCfg_Lang6Code_TextBox.Text,
                SongMgrCfg_Lang7Code_TextBox.Text,
                SongMgrCfg_Lang8Code_TextBox.Text,
                SongMgrCfg_Lang9Code_TextBox.Text,
                SongMgrCfg_Lang10Code_TextBox.Text
            };
            Global.SongMgrLangCode = string.Join(",", strlist);
        }

        private void SongMgrCfg_SongType_ListBox_Enter(object sender, EventArgs e)
        {
            SongMgrCfg_Tooltip_Label.Text = "";
            SongMgrCfg_SongType_Button.Text = "移除";
        }

        private void SongMgrCfg_SongType_TextBox_Enter(object sender, EventArgs e)
        {
            SongMgrCfg_Tooltip_Label.Text = "";
            SongMgrCfg_SongType_Button.Text = "加入";
        }

        private void SongMgrCfg_SongType_Button_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();
            switch (SongMgrCfg_SongType_Button.Text)
            {
                case "加入":
                    if (SongMgrCfg_SongType_TextBox.Text != "")
                    {
                        if (SongMgrCfg_Tooltip_Label.Text == "尚未輸入要加入的歌曲類別名稱!") SongMgrCfg_Tooltip_Label.Text = "";
                        dt = (DataTable)SongMgrCfg_SongType_ListBox.DataSource;
                        dt.Rows.Add(dt.NewRow());
                        dt.Rows[dt.Rows.Count - 1][0] = SongMgrCfg_SongType_TextBox.Text;
                        dt.Rows[dt.Rows.Count - 1][1] = dt.Rows.Count;
                        SongMgrCfg_SongType_TextBox.Text = "";
                        
                        List<string> list = new List<string>();

                        foreach (DataRow row in dt.Rows)
                        {
                            foreach (DataColumn column in dt.Columns)
                            {
                                if (row[column] != null)
                                {
                                    if (column.ColumnName == "Display")
                                    {
                                        list.Add(row[column].ToString());
                                    }
                                }
                            }
                        }
                        Global.SongMgrSongType = string.Join(",", list);
                        SongQuery_RefreshSongType();
                        SongAdd_RefreshDefaultSongType();
                    }
                    else
                    {
                        SongMgrCfg_Tooltip_Label.Text = "尚未輸入要加入的歌曲類別名稱!";
                    }
                    break;
                case "移除":
                    if (SongMgrCfg_SongType_ListBox.SelectedItem != null)
                    {
                        if (SongAdd_DefaultSongType_ComboBox.Text != SongMgrCfg_SongType_ListBox.Text)
                        {
                            int index = int.Parse(SongMgrCfg_SongType_ListBox.SelectedIndex.ToString());
                            dt = (DataTable)SongMgrCfg_SongType_ListBox.DataSource;
                            dt.Rows.RemoveAt(index);

                            List<string> list = new List<string>();

                            foreach (DataRow row in dt.Rows)
                            {
                                foreach (DataColumn column in dt.Columns)
                                {
                                    if (row[column] != null)
                                    {
                                        if (column.ColumnName == "Display")
                                        {
                                            list.Add(row[column].ToString());
                                        }
                                    }
                                }
                            }
                            Global.SongMgrSongType = string.Join(",", list);
                            SongQuery_RefreshSongType();
                            SongAdd_RefreshDefaultSongType();
                        }
                        else
                        {
                            SongMgrCfg_Tooltip_Label.Text = "此為預設的歌曲類別名稱!";
                        }
                    }
                    else
                    {
                        SongMgrCfg_Tooltip_Label.Text = "已無可以刪除的歌曲類別名稱!";
                    }
                    break;
            }
        }

        private void SongMgrCfg_LangCode_TextBox_Enter(object sender, EventArgs e)
        {
            switch (SongMgrCfg_MaxDigitCode_ComboBox.SelectedValue.ToString())
            {
                case "1":
                    ((TextBox)sender).MaxLength = 5;
                    ((TextBox)sender).ImeMode = ImeMode.Off;
                    break;
                case "2":
                    ((TextBox)sender).MaxLength = 6;
                    ((TextBox)sender).ImeMode = ImeMode.Off;
                    break;
            }
        }

        private void SongMgrCfg_LangCode_TextBox_Validating(object sender, CancelEventArgs e)
        {
            switch (Global.SongMgrMaxDigitCode)
            {
                case "1":
                    if (((TextBox)sender).Text.Length != 5)
                    {
                        e.Cancel = true;
                        SongMgrCfg_Tooltip_Label.Text = "此項目必須輸入5位數的歌曲編號";
                    }
                    else
                    {
                        if (SongMgrCfg_Tooltip_Label.Text == "此項目必須輸入5位數的歌曲編號") SongMgrCfg_Tooltip_Label.Text = "";
                    }
                    break;
                case "2":
                    if (((TextBox)sender).Text.Length != 6)
                    {
                        e.Cancel = true;
                        SongMgrCfg_Tooltip_Label.Text = "此項目必須輸入6位數的歌曲編號";
                    }
                    else
                    {
                        if (SongMgrCfg_Tooltip_Label.Text == "此項目必須輸入6位數的歌曲編號") SongMgrCfg_Tooltip_Label.Text = "";
                    }
                    break;
            }

            if (SongMgrCfg_Tooltip_Label.Text != "此項目必須輸入5位數的歌曲編號" & SongMgrCfg_Tooltip_Label.Text != "此項目必須輸入6位數的歌曲編號")
            {
                TextBox[] SongMgrCfg_LangCode_TextBox =
            {
                SongMgrCfg_Lang1Code_TextBox,
                SongMgrCfg_Lang2Code_TextBox,
                SongMgrCfg_Lang3Code_TextBox,
                SongMgrCfg_Lang4Code_TextBox,
                SongMgrCfg_Lang5Code_TextBox,
                SongMgrCfg_Lang6Code_TextBox,
                SongMgrCfg_Lang7Code_TextBox,
                SongMgrCfg_Lang8Code_TextBox,
                SongMgrCfg_Lang9Code_TextBox,
                SongMgrCfg_Lang10Code_TextBox
            };

                List<string> SongMgrCfg_LangCode_TextBoxName = new List<string>()
            {
                SongMgrCfg_Lang1Code_TextBox.Name,
                SongMgrCfg_Lang2Code_TextBox.Name,
                SongMgrCfg_Lang3Code_TextBox.Name,
                SongMgrCfg_Lang4Code_TextBox.Name,
                SongMgrCfg_Lang5Code_TextBox.Name,
                SongMgrCfg_Lang6Code_TextBox.Name,
                SongMgrCfg_Lang7Code_TextBox.Name,
                SongMgrCfg_Lang8Code_TextBox.Name,
                SongMgrCfg_Lang9Code_TextBox.Name,
                SongMgrCfg_Lang10Code_TextBox.Name
            };

                bool ValueError = false;
                int i = SongMgrCfg_LangCode_TextBoxName.IndexOf(((TextBox)sender).Name);

                if (i != 0)
                {
                    if (Convert.ToInt32(((TextBox)sender).Text) <= Convert.ToInt32(SongMgrCfg_LangCode_TextBox[i - 1].Text))
                    {
                        e.Cancel = true;
                        SongMgrCfg_Tooltip_Label.Text = "數值不能小於或等於前面語系的歌曲編號";
                    }
                    else
                    {
                        if (SongMgrCfg_Tooltip_Label.Text == "數值不能小於或等於前面語系的歌曲編號") SongMgrCfg_Tooltip_Label.Text = "";

                        foreach (TextBox tb in SongMgrCfg_LangCode_TextBox)
                        {
                            if (tb.Name != "SongMgrCfg_Lang1Code_TextBox" & SongMgrCfg_LangCode_TextBoxName.IndexOf(tb.Name) > i)
                            {
                                if (Convert.ToInt32(((TextBox)sender).Text) >= Convert.ToInt32(tb.Text))
                                {
                                    ValueError = true;
                                    break;
                                }
                            }
                        }

                        if (ValueError)
                        {
                            e.Cancel = true;
                            SongMgrCfg_Tooltip_Label.Text = "數值不能大於或等於後面語系的歌曲編號";
                        }
                        else
                        {
                            if (SongMgrCfg_Tooltip_Label.Text == "數值不能大於或等於後面語系的歌曲編號") SongMgrCfg_Tooltip_Label.Text = "";
                        }
                    }
                }
                else
                {
                    foreach (TextBox tb in SongMgrCfg_LangCode_TextBox)
                    {
                        if (tb.Name != "SongMgrCfg_Lang1Code_TextBox")
                        {
                            if (Convert.ToInt32(((TextBox)sender).Text) >= Convert.ToInt32(tb.Text))
                            {
                                ValueError = true;
                                break;
                            }
                        }
                    }

                    if (ValueError)
                    {
                        e.Cancel = true;
                        SongMgrCfg_Tooltip_Label.Text = "數值不能大於或等於後面語系的歌曲編號";
                    }
                    else
                    {
                        if (SongMgrCfg_Tooltip_Label.Text == "數值不能大於或等於後面語系的歌曲編號") SongMgrCfg_Tooltip_Label.Text = "";
                    }
                }
            }
        }

        private void SongMgrCfg_SongInfoSeparate_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (SongMgrCfg_SongInfoSeparate_ComboBox.SelectedValue.ToString())
            {
                case "1":
                case "2":
                    Global.SongMgrSongInfoSeparate = SongMgrCfg_SongInfoSeparate_ComboBox.SelectedValue.ToString();
                    break;
            }
        }

        private void SongMgrCfg_CrtchorusSeparate_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (SongMgrCfg_CrtchorusSeparate_ComboBox.SelectedValue.ToString())
            {
                case "1":
                case "2":
                    Global.SongMgrChorusSeparate = SongMgrCfg_CrtchorusSeparate_ComboBox.SelectedValue.ToString();
                    break;
            }
        }

        private void SongMgrCfg_FolderStructure_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (SongMgrCfg_FolderStructure_ComboBox.SelectedValue.ToString())
            {
                case "1":
                    SongMgrCfg_CrtchorusMerge_CheckBox.Enabled = true;
                    Global.SongMgrFolderStructure = SongMgrCfg_FolderStructure_ComboBox.SelectedValue.ToString();
                    break;
                case "2":
                    SongMgrCfg_CrtchorusMerge_CheckBox.Enabled = false;
                    Global.SongMgrFolderStructure = SongMgrCfg_FolderStructure_ComboBox.SelectedValue.ToString();
                    break;
            }
        }

        private void SongMgrCfg_FileStructure_ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch(SongMgrCfg_FileStructure_ComboBox.SelectedValue.ToString())
            {
                case "1":
                case "2":
                case "3":
                    Global.SongMgrFileStructure = SongMgrCfg_FileStructure_ComboBox.SelectedValue.ToString();
                    break;
            }
        }

        private void SongMgrCfg_SetLangLB()
        {
            Label[] SongMgrCfg_LangCode_Label =
            {
                SongMgrCfg_Lang1Code_Label,
                SongMgrCfg_Lang2Code_Label,
                SongMgrCfg_Lang3Code_Label,
                SongMgrCfg_Lang4Code_Label,
                SongMgrCfg_Lang5Code_Label,
                SongMgrCfg_Lang6Code_Label,
                SongMgrCfg_Lang7Code_Label,
                SongMgrCfg_Lang8Code_Label,
                SongMgrCfg_Lang9Code_Label,
                SongMgrCfg_Lang10Code_Label
            };

            for (int i = 0; i < SongMgrCfg_LangCode_Label.Count<Label>(); i++)
            {
                if (Global.CrazyktvSongLangList[i].Length > 2 )
                {
                    SongMgrCfg_LangCode_Label[i].Text = Global.CrazyktvSongLangList[i].Substring(0, 1) + "語:";
                }
                else
                {
                    SongMgrCfg_LangCode_Label[i].Text = Global.CrazyktvSongLangList[i] + ":";
                }
            }
        }

        private void SongMgrCfg_BackupRemoveSong_CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Global.SongMgrBackupRemoveSong = SongMgrCfg_BackupRemoveSong_CheckBox.Checked.ToString();
        }


    }
    


    
    class SongMgrCfg
    {
        public static DataTable GetSongAddModeList()
        {
            DataTable list = new DataTable();
            list.Columns.Add(new DataColumn("Display", typeof(string)));
            list.Columns.Add(new DataColumn("Value", typeof(int)));
            list.Rows.Add(list.NewRow());
            list.Rows[0][0] = "自動搬移來源 KTV 檔案至歌庫資料夾";
            list.Rows[0][1] = 1;
            list.Rows.Add(list.NewRow());
            list.Rows[1][0] = "自動複製來源 KTV 檔案至歌庫資料夾";
            list.Rows[1][1] = 2;
            return list;
        }

        public static DataTable GetMaxDigitCodeList()
        {
            DataTable list = new DataTable();
            list.Columns.Add(new DataColumn("Display", typeof(string)));
            list.Columns.Add(new DataColumn("Value", typeof(int)));
            list.Rows.Add(list.NewRow());
            list.Rows[0][0] = "5位數編碼";
            list.Rows[0][1] = 1;
            list.Rows.Add(list.NewRow());
            list.Rows[1][0] = "6位數編碼";
            list.Rows[1][1] = 2;
            return list;
        }

        public static DataTable GetSongTypeList()
        {
            switch (Global.SongMgrSongType)
            {
                case "":
                    Global.SongMgrSongType = "原聲原影,演唱會,原聲,原影,翻唱,自製,模糊,無人聲,無伴唱,消音不全";
                    break;
                case "null":
                    Global.SongMgrSongType = "";
                    break;
            }

            DataTable list = new DataTable(); 
            list.Columns.Add(new DataColumn("Display", typeof(string))); 
            list.Columns.Add(new DataColumn("Value", typeof(int))); 
            if (Global.SongMgrSongType != "")
            {
                string[] str = Global.SongMgrSongType.Split(',');
                foreach (string s in str)
                {
                    list.Rows.Add(list.NewRow());
                    list.Rows[list.Rows.Count - 1][0] = s;
                    list.Rows[list.Rows.Count - 1][1] = list.Rows.Count;
                }
            }
            return list;
        }

        public static DataTable GetSongInfoSeparateList()
        {
            DataTable list = new DataTable();
            list.Columns.Add(new DataColumn("Display", typeof(string)));
            list.Columns.Add(new DataColumn("Value", typeof(int)));
            list.Rows.Add(list.NewRow());
            list.Rows[0][0] = "_";
            list.Rows[0][1] = 1;
            list.Rows.Add(list.NewRow());
            list.Rows[1][0] = "-";
            list.Rows[1][1] = 2;
            return list;
        }

        public static DataTable GetCrtchorusSeparateList()
        {
            DataTable list = new DataTable();
            list.Columns.Add(new DataColumn("Display", typeof(string)));
            list.Columns.Add(new DataColumn("Value", typeof(int)));
            list.Rows.Add(list.NewRow());
            list.Rows[0][0] = "&";
            list.Rows[0][1] = 1;
            list.Rows.Add(list.NewRow());
            list.Rows[1][0] = "+";
            list.Rows[1][1] = 2;
            return list;
        }

        public static DataTable GetFolderStructureList()
        {
            DataTable list = new DataTable();
            list.Columns.Add(new DataColumn("Display", typeof(string)));
            list.Columns.Add(new DataColumn("Value", typeof(int)));
            list.Rows.Add(list.NewRow());
            list.Rows[0][0] = @"\語系\歌手類別\歌手";
            list.Rows[0][1] = 1;
            list.Rows.Add(list.NewRow());
            list.Rows[1][0] = @"\語系\歌手類別";
            list.Rows[1][1] = 2;
            return list;
        }

        public static DataTable GetFileStructureList()
        {
            DataTable list = new DataTable();
            list.Columns.Add(new DataColumn("Display", typeof(string)));
            list.Columns.Add(new DataColumn("Value", typeof(int)));
            
            switch(Global.SongMgrFolderStructure)
            {
                case "1":
                case "2":
                    list.Rows.Add(list.NewRow());
                    list.Rows[0][0] = "歌手_歌名";
                    list.Rows[0][1] = 1;
                    list.Rows.Add(list.NewRow());
                    list.Rows[1][0] = "歌名_歌手";
                    list.Rows[1][1] = 2;
                    list.Rows.Add(list.NewRow());
                    list.Rows[2][0] = "歌曲編號_歌手_歌名";
                    list.Rows[2][1] = 3;
                    break;
            }
            return list;
        }



    
    }
}