﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrazyKTV_SongMgr
{
    public partial class MainFrom : Form
    {
        private void SongMaintenance_SingerSpellCorrect_Button_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("你確定要校正歌手拼音嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Global.TimerStartTime = DateTime.Now;
                Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                SongMaintenance.CreateSongDataTable();
                Common_SwitchSetUI(false);

                SongMaintenance_Tooltip_Label.Text = "正在解析歌手的拼音資料,請稍待...";

                var tasks = new List<Task>();
                tasks.Add(Task.Factory.StartNew(() => SongMaintenance_SpellCorrectTask("ktv_Singer")));
                tasks.Add(Task.Factory.StartNew(() => SongMaintenance_SpellCorrectTask("ktv_AllSinger")));

                Task.Factory.ContinueWhenAll(tasks.ToArray(), SpellCorrectEndTask =>
                {
                    Global.TimerEndTime = DateTime.Now;
                    this.BeginInvoke((Action)delegate()
                    {
                        SongMaintenance_Tooltip_Label.Text = "總共更新 " + Global.TotalList[0] + " 位歌庫歌手及 " + Global.TotalList[1] + " 位預設歌手的拼音資料,失敗 " + Global.TotalList[2] + " 位,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                        Common_SwitchSetUI(true);
                    });
                    SongMaintenance.DisposeSongDataTable();
                });
            }
        }

        private void SongMaintenance_SongSpellCorrect_Button_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("你確定要校正歌曲拼音嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Global.TimerStartTime = DateTime.Now;
                Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                SongMaintenance.CreateSongDataTable();
                Common_SwitchSetUI(false);

                SongMaintenance_Tooltip_Label.Text = "正在解析歌曲的拼音資料,請稍待...";

                var tasks = new List<Task>();
                tasks.Add(Task.Factory.StartNew(() => SongMaintenance_SpellCorrectTask("ktv_Song")));

                Task.Factory.ContinueWhenAll(tasks.ToArray(), SpellCorrectEndTask =>
                {
                    this.BeginInvoke((Action)delegate()
                    {
                        Global.TimerEndTime = DateTime.Now;
                        SongMaintenance_Tooltip_Label.Text = "總共更新 " + Global.TotalList[0] + " 首歌曲的拼音資料,失敗 " + Global.TotalList[1] + " 首,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                        Common_SwitchSetUI(true);
                        SongMaintenance.DisposeSongDataTable();
                    });
                });
            }
        }

        private void SongMaintenance_SpellCorrectTask(object TableName)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            List<string> list = new List<string>();
            List<string> SpellList = new List<string>();

            switch ((string)TableName)
            {
                case "ktv_Singer":
                    if (Global.SingerDT.Rows.Count > 0)
                    {
                        foreach (DataRow row in Global.SingerDT.AsEnumerable())
                        {
                            string str = "";
                            SpellList = new List<string>();

                            str = row["Singer_Id"].ToString() + "*";
                            str += row["Singer_Name"].ToString() + "*";
                            SpellList = CommonFunc.GetSongNameSpell(row["Singer_Name"].ToString());
                            if (SpellList[2] == "") SpellList[2] = "0";
                            str += SpellList[0] + "*" + SpellList[2] + "*" + SpellList[1] + "*" + SpellList[3];
                            list.Add(str);
                        }

                        OleDbConnection conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
                        OleDbCommand cmd = new OleDbCommand();
                        string sqlColumnStr = "Singer_Spell = @SingerSpell, Singer_Strokes = @SingerStrokes, Singer_SpellNum = @SingerSpellNum, Singer_PenStyle = @SingerPenStyle";
                        string SongUpdateSqlStr = "update ktv_Singer set " + sqlColumnStr + " where Singer_Id=@SingerId";
                        cmd = new OleDbCommand(SongUpdateSqlStr, conn);
                        List<string> valuelist = new List<string>();

                        foreach (string str in list)
                        {
                            valuelist = new List<string>(str.Split('*'));

                            cmd.Parameters.AddWithValue("@SingerSpell", valuelist[2]);
                            cmd.Parameters.AddWithValue("@SingerStrokes", valuelist[3]);
                            cmd.Parameters.AddWithValue("@SingerSpellNum", valuelist[4]);
                            cmd.Parameters.AddWithValue("@SingerPenStyle", valuelist[5]);
                            cmd.Parameters.AddWithValue("@SingerId", valuelist[0]);

                            try
                            {
                                lock (LockThis)
                                {
                                    Global.TotalList[0]++;
                                }
                                cmd.ExecuteNonQuery();
                            }
                            catch
                            {
                                lock (LockThis)
                                {
                                    Global.TotalList[2]++;
                                }
                                Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                                Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【拼音校正】更新資料庫時發生錯誤: " + str;
                                Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                            }
                            cmd.Parameters.Clear();
                            
                            this.BeginInvoke((Action)delegate()
                            {
                                SongMaintenance_Tooltip_Label.Text = "正在更新第 " + Global.TotalList[0] + " 位歌庫歌手及 " + Global.TotalList[1] + " 位預設歌手的拼音資料,請稍待...";
                            });
                        }
                        conn.Close();
                        list.Clear();
                    }
                    break;
                case "ktv_AllSinger":
                    if (Global.AllSingerDT.Rows.Count > 0)
                    {
                        foreach (DataRow row in Global.AllSingerDT.AsEnumerable())
                        {
                            string str = "";
                            SpellList = new List<string>();

                            str = row["Singer_Id"].ToString() + "*";
                            str += row["Singer_Name"].ToString() + "*";
                            SpellList = CommonFunc.GetSongNameSpell(row["Singer_Name"].ToString());
                            if (SpellList[2] == "") SpellList[2] = "0";
                            str += SpellList[0] + "*" + SpellList[2] + "*" + SpellList[1] + "*" + SpellList[3];
                            list.Add(str);
                        }

                        OleDbConnection conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
                        OleDbCommand cmd = new OleDbCommand();
                        string sqlColumnStr = "Singer_Spell = @SingerSpell, Singer_Strokes = @SingerStrokes, Singer_SpellNum = @SingerSpellNum, Singer_PenStyle = @SingerPenStyle";
                        string SongUpdateSqlStr = "update ktv_AllSinger set " + sqlColumnStr + " where Singer_Id=@SingerId";
                        cmd = new OleDbCommand(SongUpdateSqlStr, conn);
                        List<string> valuelist = new List<string>();

                        foreach (string str in list)
                        {
                            valuelist = new List<string>(str.Split('*'));

                            cmd.Parameters.AddWithValue("@SingerSpell", valuelist[2]);
                            cmd.Parameters.AddWithValue("@SingerStrokes", valuelist[3]);
                            cmd.Parameters.AddWithValue("@SingerSpellNum", valuelist[4]);
                            cmd.Parameters.AddWithValue("@SingerPenStyle", valuelist[5]);
                            cmd.Parameters.AddWithValue("@SingerId", valuelist[0]);

                            try
                            {
                                lock (LockThis)
                                {
                                    Global.TotalList[1]++;
                                }
                                cmd.ExecuteNonQuery();
                            }
                            catch
                            {
                                lock (LockThis)
                                {
                                    Global.TotalList[2]++;
                                }
                                Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                                Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【拼音校正】更新資料庫時發生錯誤: " + str;
                                Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                            }
                            cmd.Parameters.Clear();

                            this.BeginInvoke((Action)delegate()
                            {
                                SongMaintenance_Tooltip_Label.Text = "正在更新第 " + Global.TotalList[0] + " 位歌庫歌手及 " + Global.TotalList[1] + " 位預設歌手的拼音資料,請稍待...";
                            });
                        }
                        conn.Close();
                        list.Clear();
                    }
                    break;
                case "ktv_Song":
                    if (Global.SongDT.Rows.Count > 0)
                    {
                        foreach (DataRow row in Global.SongDT.AsEnumerable())
                        {
                            string str = "";
                            SpellList = new List<string>();

                            str = row["Song_Id"].ToString() + "*";
                            str += row["Song_SongName"].ToString() + "*";
                            SpellList = CommonFunc.GetSongNameSpell(row["Song_SongName"].ToString());
                            if (SpellList[2] == "") SpellList[2] = "0";
                            str += SpellList[0] + "*" + SpellList[1] + "*" + SpellList[2] + "*" + SpellList[3];
                            list.Add(str);
                            this.BeginInvoke((Action)delegate()
                            {
                                SongMaintenance_Tooltip_Label.Text = "正在解析第 " + list.Count + " 首歌曲的拼音資料,請稍待...";
                            });
                        }

                        OleDbConnection conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
                        OleDbCommand cmd = new OleDbCommand();
                        string sqlColumnStr = "Song_Spell = @SongSpell, Song_SpellNum = @SongSpellNum, Song_SongStroke = @SongSongStroke, Song_PenStyle = @SongPenStyle";
                        string SongUpdateSqlStr = "update ktv_Song set " + sqlColumnStr + " where Song_Id=@SongId";
                        cmd = new OleDbCommand(SongUpdateSqlStr, conn);
                        List<string> valuelist = new List<string>();

                        foreach (string str in list)
                        {
                            valuelist = new List<string>(str.Split('*'));

                            cmd.Parameters.AddWithValue("@SongSpell", valuelist[2]);
                            cmd.Parameters.AddWithValue("@SongSpellNum", valuelist[3]);
                            cmd.Parameters.AddWithValue("@SongSongStroke", valuelist[4]);
                            cmd.Parameters.AddWithValue("@SongPenStyle", valuelist[5]);
                            cmd.Parameters.AddWithValue("@SongId", valuelist[0]);

                            try
                            {
                                cmd.ExecuteNonQuery();
                                lock (LockThis)
                                {
                                    Global.TotalList[0]++;
                                }
                            }
                            catch
                            {
                                lock (LockThis)
                                {
                                    Global.TotalList[1]++;
                                }
                                Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                                Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【拼音校正】更新資料庫時發生錯誤: " + str;
                                Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                            }
                            cmd.Parameters.Clear();

                            this.BeginInvoke((Action)delegate()
                            {
                                SongMaintenance_Tooltip_Label.Text = "正在更新第 " + Global.TotalList[0] + " 首歌曲的拼音資料,請稍待...";
                            });
                        }
                        conn.Close();
                        list.Clear();
                    }
                    break;
            }

            if ((string)TableName != "ktv_Song")
            {

            }
        }

        private void SongMaintenance_CodeConvTo5_Button_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("你確定要轉換為 5 位數編碼嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Global.TimerStartTime = DateTime.Now;
                Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                SongMaintenance.CreateSongDataTable();
                Common_SwitchSetUI(false);

                if (Global.SongMgrMaxDigitCode != "1") SongMgrCfg_MaxDigitCode_ComboBox.SelectedValue = 1;
                SongMgrCfg_GetSongMgrLangCode();

                var tasks = new List<Task>();
                tasks.Add(Task.Factory.StartNew(() => SongMaintenance_CodeConvTask()));

                Task.Factory.ContinueWhenAll(tasks.ToArray(), CodeConvEndTask =>
                {
                    this.BeginInvoke((Action)delegate()
                    {
                        Global.TimerEndTime = DateTime.Now;
                        SongMaintenance_Tooltip_Label.Text = "總共轉換 " + Global.TotalList[0] + " 首歌曲的歌曲編號,失敗 " + Global.TotalList[1] + " 首,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                        Common_CheckDBVer();
                        Common_SwitchSetUI(true);
                        SongMaintenance.DisposeSongDataTable();
                    });
                });
            }
        }

        private void SongMaintenance_CodeConvTo6_Button_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("你確定要轉換為 6 位數編碼嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Global.TimerStartTime = DateTime.Now;
                Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                SongMaintenance.CreateSongDataTable();
                Common_SwitchSetUI(false);

                if (Global.SongMgrMaxDigitCode != "2") SongMgrCfg_MaxDigitCode_ComboBox.SelectedValue = 2;
                SongMgrCfg_GetSongMgrLangCode();

                var tasks = new List<Task>();
                tasks.Add(Task.Factory.StartNew(() => SongMaintenance_CodeConvTask()));

                Task.Factory.ContinueWhenAll(tasks.ToArray(), CodeConvEndTask =>
                {
                    this.BeginInvoke((Action)delegate()
                    {
                        Global.TimerEndTime = DateTime.Now;
                        SongMaintenance_Tooltip_Label.Text = "總共轉換 " + Global.TotalList[0] + " 首歌曲的歌曲編號,失敗 " + Global.TotalList[1] + " 首,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                        Common_CheckDBVer();
                        Common_SwitchSetUI(true);
                        SongMaintenance.DisposeSongDataTable();
                    });
                });
            }
        }

        private void SongMaintenance_CodeConvTask()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;

            string MaxDigitCode = "";
            if (Global.SongMgrMaxDigitCode == "1") { MaxDigitCode = "D5"; } else { MaxDigitCode = "D6"; }

            List<string> list = new List<string>();
            List<string> StrIdlist = new List<string>();
            StrIdlist = new List<string>(Regex.Split(Global.SongMgrLangCode, ",", RegexOptions.None));
            List<int> Idlist = new List<int>();
            Idlist = StrIdlist.Select(s => Convert.ToInt32(s)).ToList();

            if (Global.SongDT.Rows.Count > 0)
            {
                foreach (DataRow row in Global.SongDT.AsEnumerable())
                {
                    string str = "";
                    if (CommonFunc.GetSongLangStr(0, 0, row["Song_Lang"].ToString()) != "-1")
                    {
                        int LangIndex = Convert.ToInt32(CommonFunc.GetSongLangStr(0, 0, row["Song_Lang"].ToString()));
                        string NewSongId = Idlist[LangIndex].ToString(MaxDigitCode);
                        Idlist[LangIndex]++;

                        str = row["Song_Id"].ToString() + "*";
                        str += NewSongId + "*";
                        str += row["Song_SongName"].ToString() + "*";
                        str += row["Song_Lang"].ToString();
                        list.Add(str);
                    }
                    else
                    {
                        lock (LockThis)
                        {
                            Global.TotalList[1]++;
                        }
                        str = row["Song_Id"].ToString() + "*";
                        str += row["Song_SongName"].ToString() + "*";
                        str += row["Song_Lang"].ToString();

                        Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                        Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【編碼位數轉換】此首歌曲的語系資料不正確: " + str;
                        Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                    }
                    
                    this.BeginInvoke((Action)delegate()
                    {
                        SongMaintenance_Tooltip_Label.Text = "正在配發第 " + Global.SongDT.Rows.IndexOf(row) + " 首歌曲的歌曲編號,請稍待...";
                    });
                }
            }

            OleDbConnection conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
            OleDbCommand cmd = new OleDbCommand();
            string sqlColumnStr = "Song_Id = @SongId";
            string SongUpdateSqlStr = "update ktv_Song set " + sqlColumnStr + " where Song_Id=@OldSongId and Song_SongName = @SongSongName and Song_Lang = @SongLang";
            cmd = new OleDbCommand(SongUpdateSqlStr, conn);
            List<string> valuelist = new List<string>();

            foreach (string str in list)
            {
                valuelist = new List<string>(str.Split('*'));

                cmd.Parameters.AddWithValue("@SongId", valuelist[1]);
                cmd.Parameters.AddWithValue("@OldSongId", valuelist[0]);
                cmd.Parameters.AddWithValue("@SongSongName", valuelist[2]);
                cmd.Parameters.AddWithValue("@SongLang", valuelist[3]);

                try
                {
                    cmd.ExecuteNonQuery();
                    lock (LockThis)
                    {
                        Global.TotalList[0]++;
                    }
                }
                catch
                {
                    lock (LockThis)
                    {
                        Global.TotalList[1]++;
                    }
                    Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【編碼位數轉換】更新資料庫時發生錯誤: " + str;
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                }
                cmd.Parameters.Clear();

                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在轉換第 " + Global.TotalList[0] + " 首歌曲的歌曲編號,請稍待...";
                });
            }
            conn.Close();
            list.Clear();
        }

        private void SongMaintenance_CodeCorrect_Button_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("你確定要校正編碼位數嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Global.TimerStartTime = DateTime.Now;
                Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                SongMaintenance.CreateSongDataTable();
                Common_SwitchSetUI(false);

                var d5code = from row in Global.SongDT.AsEnumerable()
                             where row.Field<string>("Song_Id").Length == 5
                             select row;

                var d6code = from row in Global.SongDT.AsEnumerable()
                             where row.Field<string>("Song_Id").Length == 6
                             select row;

                if (d5code.Count<DataRow>() > d6code.Count<DataRow>())
                {
                    if (Global.SongMgrMaxDigitCode != "1") SongMgrCfg_MaxDigitCode_ComboBox.SelectedValue = 1;
                    CommonFunc.GetMaxSongId(5);
                    CommonFunc.GetNotExistsSongId(5);
                }
                else
                {
                    if (Global.SongMgrMaxDigitCode != "2") SongMgrCfg_MaxDigitCode_ComboBox.SelectedValue = 2;
                    CommonFunc.GetMaxSongId(6);
                    CommonFunc.GetNotExistsSongId(6);
                }
                SongMgrCfg_GetSongMgrLangCode();

                var tasks = new List<Task>();
                tasks.Add(Task.Factory.StartNew(() => SongMaintenance_CodeCorrectTask()));

                Task.Factory.ContinueWhenAll(tasks.ToArray(), CodeCorrectEndTask =>
                {
                    this.BeginInvoke((Action)delegate()
                    {
                        Global.TimerEndTime = DateTime.Now;
                        SongMaintenance_Tooltip_Label.Text = "總共校正 " + Global.TotalList[0] + " 首歌曲的歌曲編號,失敗 " + Global.TotalList[1] + " 首,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                        Common_CheckDBVer();
                        Common_SwitchSetUI(true);
                        SongMaintenance.DisposeSongDataTable();
                    });
                });
            }
        }

        private void SongMaintenance_CodeCorrectTask()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;

            int CorrectDigitCode = 0;

            if (Global.SongMgrMaxDigitCode == "1") { CorrectDigitCode = 5; } else { CorrectDigitCode = 6; }

            List<string> list = new List<string>();

            if (Global.SongDT.Rows.Count > 0)
            {
                var query = from row in Global.SongDT.AsEnumerable()
                            where row.Field<string>("Song_Id").Length != CorrectDigitCode
                            select row;

                foreach (DataRow row in query)
                {
                    string str = "";
                    if (CommonFunc.GetSongLangStr(0, 0, row["Song_Lang"].ToString()) != "-1")
                    {
                        string LangStr = row["Song_Lang"].ToString();
                        string NewSongId = SongMaintenance.GetNextSongId(LangStr);

                        str = row["Song_Id"].ToString() + "*";
                        str += NewSongId + "*";
                        str += row["Song_SongName"].ToString() + "*";
                        str += row["Song_Lang"].ToString();
                        list.Add(str);
                    }
                    else
                    {
                        lock (LockThis)
                        {
                            Global.TotalList[1]++;
                        }
                        str = row["Song_Id"].ToString() + "*";
                        str += row["Song_SongName"].ToString() + "*";
                        str += row["Song_Lang"].ToString();

                        Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                        Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【編碼位數轉換】此首歌曲的語系資料不正確: " + str;
                        Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                    }

                    this.BeginInvoke((Action)delegate()
                    {
                        SongMaintenance_Tooltip_Label.Text = "正在配發第 " + Global.SongDT.Rows.IndexOf(row) + " 首歌曲的歌曲編號,請稍待...";
                    });
                }
            }

            OleDbConnection conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
            OleDbCommand cmd = new OleDbCommand();
            string sqlColumnStr = "Song_Id = @SongId";
            string SongUpdateSqlStr = "update ktv_Song set " + sqlColumnStr + " where Song_Id=@OldSongId and Song_SongName = @SongSongName and Song_Lang = @SongLang";
            cmd = new OleDbCommand(SongUpdateSqlStr, conn);
            List<string> valuelist = new List<string>();

            foreach (string str in list)
            {
                valuelist = new List<string>(str.Split('*'));

                cmd.Parameters.AddWithValue("@SongId", valuelist[1]);
                cmd.Parameters.AddWithValue("@OldSongId", valuelist[0]);
                cmd.Parameters.AddWithValue("@SongSongName", valuelist[2]);
                cmd.Parameters.AddWithValue("@SongLang", valuelist[3]);

                try
                {
                    cmd.ExecuteNonQuery();
                    lock (LockThis)
                    {
                        Global.TotalList[0]++;
                    }
                }
                catch
                {
                    lock (LockThis)
                    {
                        Global.TotalList[1]++;
                    }
                    Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【編碼位數轉換】更新資料庫時發生錯誤: " + str;
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                }
                cmd.Parameters.Clear();

                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在轉換第 " + Global.TotalList[0] + " 首歌曲的歌曲編號,請稍待...";
                });
            }
            conn.Close();
            list.Clear();
        }

        private void SongMaintenance_LRTrackExchange_Button_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("你確定要互換左右聲道數值嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Global.TimerStartTime = DateTime.Now;
                Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                SongMaintenance.CreateSongDataTable();
                Common_SwitchSetUI(false);

                var tasks = new List<Task>();
                tasks.Add(Task.Factory.StartNew(() => SongMaintenance_LRTrackExchangeTask()));

                Task.Factory.ContinueWhenAll(tasks.ToArray(), LRTrackExchangeEndTask =>
                {
                    Global.TimerEndTime = DateTime.Now;
                    this.BeginInvoke((Action)delegate()
                    {
                        SongMaintenance_Tooltip_Label.Text = "總共更新 " + Global.TotalList[0] + " 首歌曲的聲道資料,失敗 " + Global.TotalList[1] + " 首,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                        Common_SwitchSetUI(true);
                    });
                    SongMaintenance.DisposeSongDataTable();
                });
            }
        }

        private void SongMaintenance_LRTrackExchangeTask()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            List<string> list = new List<string>();

            var query = from row in Global.SongDT.AsEnumerable()
                         where row.Field<byte>("Song_Track").Equals(1) ||
                               row.Field<byte>("Song_Track").Equals(2)
                         select row;

            foreach (DataRow row in query)
            {
                switch (row["Song_Track"].ToString())
                {
                    case "1":
                        list.Add("2*" + row["Song_Id"].ToString());
                        break;
                    case "2":
                        list.Add("1*" + row["Song_Id"].ToString());
                        break;
                }
                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在解析第 " + list.Count + " 首歌曲的聲道資料,請稍待...";
                });
            }

            OleDbConnection conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
            OleDbCommand cmd = new OleDbCommand();
            string sqlColumnStr = "Song_Track = @SongTrack";
            string SongUpdateSqlStr = "update ktv_Song set " + sqlColumnStr + " where Song_Id=@SongId";
            cmd = new OleDbCommand(SongUpdateSqlStr, conn);
            List<string> valuelist = new List<string>();

            foreach (string str in list)
            {
                valuelist = new List<string>(str.Split('*'));

                cmd.Parameters.AddWithValue("@SongTrack", valuelist[0]);
                cmd.Parameters.AddWithValue("@SongId", valuelist[1]);

                try
                {
                    cmd.ExecuteNonQuery();
                    lock (LockThis)
                    {
                        Global.TotalList[0]++;
                    }
                }
                catch
                {
                    lock (LockThis)
                    {
                        Global.TotalList[1]++;
                    }
                    Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【互換左右聲道】更新資料庫時發生錯誤: " + str;
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                }
                cmd.Parameters.Clear();

                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在轉換第 " + Global.TotalList[0] + " 首歌曲的聲道資料,請稍待...";
                });
            }
            conn.Close();
            list.Clear();
        }

        private void SongMaintenance_VolumeChange_TextBox_Validating(object sender, CancelEventArgs e)
        {
            if (string.IsNullOrEmpty(((TextBox)sender).Text))
            {
                SongMaintenance_Tooltip_Label.Text = "此項目的值不能為空白!";
                e.Cancel = true;
            }
            else
            {
                if (int.Parse(((TextBox)sender).Text) > 100)
                {
                    SongMaintenance_Tooltip_Label.Text = "此項目只能輸入 0 ~ 100 的值!";
                    e.Cancel = true;
                }
                else
                {
                    SongMaintenance_Tooltip_Label.Text = "";
                }
            }
        }

        private void SongMaintenance_VolumeChange_Button_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("你確定要變更音量嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Global.TimerStartTime = DateTime.Now;
                Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                SongMaintenance.CreateSongDataTable();
                Common_SwitchSetUI(false);

                var tasks = new List<Task>();
                tasks.Add(Task.Factory.StartNew(() => SongMaintenance_VolumeChangeTask(SongMaintenance_VolumeChange_TextBox.Text)));

                Task.Factory.ContinueWhenAll(tasks.ToArray(), VolumeChangeEndTask =>
                {
                    Global.TimerEndTime = DateTime.Now;
                    this.BeginInvoke((Action)delegate()
                    {
                        SongMaintenance_Tooltip_Label.Text = "總共更新 " + Global.TotalList[0] + " 首歌曲的音量資料,失敗 " + Global.TotalList[1] + " 首,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                        Common_SwitchSetUI(true);
                    });
                    SongMaintenance.DisposeSongDataTable();
                });
            }
        }

        private void SongMaintenance_VolumeChangeTask(object VolumeValue)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            List<string> list = new List<string>();
            
            foreach (DataRow row in Global.SongDT.AsEnumerable())
            {
                list.Add((string)VolumeValue + "*" + row["Song_Id"].ToString());
                
                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在解析第 " + list.Count + " 首歌曲的音量資料,請稍待...";
                });
            }

            OleDbConnection conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
            OleDbCommand cmd = new OleDbCommand();
            string sqlColumnStr = "Song_Volume = @SongVolume";
            string SongUpdateSqlStr = "update ktv_Song set " + sqlColumnStr + " where Song_Id=@SongId";
            cmd = new OleDbCommand(SongUpdateSqlStr, conn);
            List<string> valuelist = new List<string>();

            foreach (string str in list)
            {
                valuelist = new List<string>(str.Split('*'));

                cmd.Parameters.AddWithValue("@SongVolume", valuelist[0]);
                cmd.Parameters.AddWithValue("@SongId", valuelist[1]);

                try
                {
                    cmd.ExecuteNonQuery();
                    lock (LockThis)
                    {
                        Global.TotalList[0]++;
                    }
                }
                catch
                {
                    lock (LockThis)
                    {
                        Global.TotalList[1]++;
                    }
                    Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【變更歌曲音量】更新資料庫時發生錯誤: " + str;
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                }
                cmd.Parameters.Clear();

                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在轉換第 " + Global.TotalList[0] + " 首歌曲的音量資料,請稍待...";
                });
            }
            conn.Close();
            list.Clear();
        }

        private void SongMaintenance_GetFavoriteUserList()
        {
            SongMaintenance_Favorite_ListBox.DataSource = CommonFunc.GetFavoriteUserList(1);
            SongMaintenance_Favorite_ListBox.DisplayMember = "Display";
            SongMaintenance_Favorite_ListBox.ValueMember = "Value";
            SongMaintenance_Favorite_ListBox.SelectedValue = 1;
        }

        private void SongMaintenance_Favorite_ListBox_Enter(object sender, EventArgs e)
        {
            SongMaintenance_Tooltip_Label.Text = "";
            SongMaintenance_Favorite_Button.Text = "移除";
        }

        private void SongMaintenance_Favorite_TextBox_Enter(object sender, EventArgs e)
        {
            SongMaintenance_Favorite_TextBox.ImeMode = ImeMode.OnHalf;
            SongMaintenance_Tooltip_Label.Text = "";
            SongMaintenance_Favorite_Button.Text = "加入";
        }

        private void SongMaintenance_Favorite_Button_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();
            string SongQuerySqlStr = "";
            string UserId = "";
            string UserName = "";

            OleDbConnection conn = new OleDbConnection();
            OleDbCommand cmd = new OleDbCommand();

            switch (SongMaintenance_Favorite_Button.Text)
            {
                case "加入":
                    if (SongMaintenance_Favorite_TextBox.Text != "")
                    {
                        if (SongMaintenance_Tooltip_Label.Text == "尚未輸入要加入的最愛用戶名稱!") SongMaintenance_Tooltip_Label.Text = "";
                        int AddUser = 1;
                        dt = (DataTable)SongMaintenance_Favorite_ListBox.DataSource;

                        if (dt.Rows.Count > 0)
                        {
                            if (SongMaintenance_Tooltip_Label.Text == "已有此最愛用戶名稱!") SongMaintenance_Tooltip_Label.Text = "";
                            foreach (DataRow row in dt.AsEnumerable())
                            {
                                if (row["Display"].ToString() == SongMaintenance_Favorite_TextBox.Text)
                                {
                                    SongMaintenance_Tooltip_Label.Text = "已有此最愛用戶名稱!";
                                    dt = new DataTable();
                                    AddUser = 0;
                                    break;
                                }
                            }
                        }
                        if (AddUser != 0)
                        {
                            SongQuerySqlStr = "select User_Id, User_Name from ktv_User";
                            dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongQuerySqlStr, "");

                            for (int i = 1; i < 999; i++)
                            {
                                int AddUserID = 1;
                                foreach (DataRow row in dt.AsEnumerable())
                                {
                                    if (row["User_Id"].ToString() == "U" + i.ToString("D3"))
                                    {
                                        AddUserID = 0;
                                        break;
                                    }
                                }

                                if (AddUserID != 0)
                                {
                                    UserId = "U" + i.ToString("D3");
                                    UserName = SongMaintenance_Favorite_TextBox.Text;
                                    break;
                                }
                            }
                            if (UserId != "")
                            {
                                conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
                                string sqlColumnStr = "User_Id, User_Name";
                                string sqlValuesStr = "@UserId, @UserName";
                                string UserAddSqlStr = "insert into ktv_User ( " + sqlColumnStr + " ) values ( " + sqlValuesStr + " )";
                                cmd = new OleDbCommand(UserAddSqlStr, conn);

                                cmd.Parameters.AddWithValue("@UserId", UserId);
                                cmd.Parameters.AddWithValue("@UserName", UserName);
                                cmd.ExecuteNonQuery();
                                cmd.Parameters.Clear();
                                conn.Close();
                                SongMaintenance_Favorite_TextBox.Text = "";

                                Global.SongQueryFavoriteQuery = "False";
                                SongQuery_GetFavoriteUserList();
                                SongMaintenance_GetFavoriteUserList();
                                if (Global.SongQueryQueryType == "FavoriteQuery")
                                {
                                    Global.SongQueryQueryType = "SongQuery";
                                    SongQuery_EditMode_CheckBox.Enabled = true;
                                    SongQuery_DataGridView.DataSource = null;
                                    if (SongQuery_DataGridView.Columns.Count > 0) SongQuery_DataGridView.Columns.Remove("Song_FullPath");
                                    SongQuery_QueryStatus_Label.Text = "";
                                }
                            }
                            else
                            {
                                SongMaintenance_Tooltip_Label.Text = "最愛用戶數量已滿!";
                            }
                        }
                    }
                    else
                    {
                        SongMaintenance_Tooltip_Label.Text = "尚未輸入要加入的最愛用戶名稱!";
                    }
                    break;
                case "移除":
                    UserName = SongMaintenance_Favorite_ListBox.Text;
                    if (UserName == "")
                    {
                        SongMaintenance_Tooltip_Label.Text = "已無最愛用戶可移除!";
                    }
                    else
                    {
                        if (SongMaintenance_Tooltip_Label.Text == "已無最愛用戶可移除!") SongMaintenance_Tooltip_Label.Text = "";
                        if (MessageBox.Show("你確定要移除此最愛用戶【" + UserName + "】及其所有最愛歌曲嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            SongQuerySqlStr = "select User_Id, User_Name from ktv_User";
                            dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongQuerySqlStr, "");

                            var query = from row in dt.AsEnumerable()
                                        where row.Field<string>("User_Name").Equals(UserName)
                                        select row;

                            if (query.Count<DataRow>() > 0)
                            {
                                foreach (DataRow row in query)
                                {
                                    UserId = row["User_Id"].ToString();
                                    break;
                                }
                            }

                            if (UserId != "")
                            {
                                conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
                                string UserRemoveSqlStr = "delete from ktv_User where User_Id=@UserId and User_Name=@UserName";
                                cmd = new OleDbCommand(UserRemoveSqlStr, conn);

                                cmd.Parameters.AddWithValue("@UserId", UserId);
                                cmd.Parameters.AddWithValue("@UserName", UserName);
                                cmd.ExecuteNonQuery();
                                cmd.Parameters.Clear();

                                SongQuerySqlStr = "select User_Id, Song_Id from ktv_Favorite";
                                dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongQuerySqlStr, "");

                                List<string> list = new List<string>();

                                var dtquery = from row in dt.AsEnumerable()
                                              where row.Field<string>("User_Id").Equals(UserId)
                                              select row;

                                if (dtquery.Count<DataRow>() > 0)
                                {
                                    foreach (DataRow row in dtquery)
                                    {
                                        list.Add(row["Song_Id"].ToString());
                                    }


                                    string FavoriteRemoveSqlStr = "delete from ktv_Favorite where User_Id=@UserId and Song_Id=@SongId";
                                    cmd = new OleDbCommand(FavoriteRemoveSqlStr, conn);

                                    foreach (string SongId in list)
                                    {
                                        cmd.Parameters.AddWithValue("@UserId", UserId);
                                        cmd.Parameters.AddWithValue("@SongId", SongId);
                                        cmd.ExecuteNonQuery();
                                        cmd.Parameters.Clear();
                                    }
                                }
                                conn.Close();
                                Global.SongQueryFavoriteQuery = "False";
                                SongQuery_GetFavoriteUserList();
                                SongMaintenance_GetFavoriteUserList();
                                if (Global.SongQueryQueryType == "FavoriteQuery")
                                {
                                    Global.SongQueryQueryType = "SongQuery";
                                    SongQuery_EditMode_CheckBox.Enabled = true;
                                    SongQuery_DataGridView.DataSource = null;
                                    if (SongQuery_DataGridView.Columns.Count > 0) SongQuery_DataGridView.Columns.Remove("Song_FullPath");
                                    SongQuery_QueryStatus_Label.Text = "";
                                }
                            }
                        }
                    }
                    break;
            }
            dt.Dispose();
        }

        private void SongMaintenance_FavoriteExport_Button_Click(object sender, EventArgs e)
        {
            SongMaintenance.CreateSongDataTable();
            List<string> list = new List<string>();
            string SongQuerySqlStr = "";
            DataTable dt = new DataTable();

            SongQuerySqlStr = "select User_Id, User_Name from ktv_User";
            dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongQuerySqlStr, "");

            if (dt.Rows.Count > 0)
            {
                foreach(DataRow row in dt.AsEnumerable())
                {
                    list.Add("ktv_User," + row["User_Id"].ToString() + "," + row["User_Name"].ToString());
                }
            }

            SongQuerySqlStr = "select User_Id, Song_Id from ktv_Favorite";
            dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongQuerySqlStr, "");

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.AsEnumerable())
                {
                    var query = from QueryRow in Global.SongDT.AsEnumerable()
                                where QueryRow.Field<string>("Song_Id").Equals(row["Song_Id"].ToString())
                                select QueryRow;

                    if (query.Count<DataRow>() > 0)
                    {
                        foreach (DataRow songrow in query)
                        {
                            list.Add("ktv_Favorite," + row["User_Id"].ToString() + "," + songrow["Song_Lang"].ToString() + "," + songrow["Song_Singer"].ToString() + "," + songrow["Song_SongName"].ToString());
                            break;
                        }
                    }
                }
            }

            if (!Directory.Exists(Application.StartupPath + @"\SongMgr\Backup")) Directory.CreateDirectory(Application.StartupPath + @"\SongMgr\Backup");
            StreamWriter sw = new StreamWriter(Application.StartupPath + @"\SongMgr\Backup\Favorite.txt");
            foreach (string str in list)
            {
                sw.WriteLine(str);
            }

            SongMaintenance_Tooltip_Label.Text = @"已將我的最愛資料匯出至【SongMgr\Backup\Favorite.txt】檔案。";
            sw.Close();
            dt.Dispose();
            SongMaintenance.DisposeSongDataTable();
        }

        private void SongMaintenance_FavoriteImport_Button_Click(object sender, EventArgs e)
        {
            if (File.Exists(Application.StartupPath + @"\SongMgr\Backup\Favorite.txt"))
            {
                if (SongMaintenance_Tooltip_Label.Text == @"【SongMgr\Backup\Favorite.txt】我的最愛備份檔案不存在!") SongMaintenance_Tooltip_Label.Text = "";
                if (MessageBox.Show("你確定要重置並匯入我的最愛嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Global.TimerStartTime = DateTime.Now;
                    Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                    SongMaintenance.CreateSongDataTable();
                    Common_SwitchSetUI(false);

                    SongMaintenance_Tooltip_Label.Text = "正在匯入我的最愛,請稍待...";

                    var tasks = new List<Task>();
                    tasks.Add(Task.Factory.StartNew(() => SongMaintenance_FavoriteImportTask()));

                    Task.Factory.ContinueWhenAll(tasks.ToArray(), FavoriteImportEndTask =>
                    {
                        Global.TimerEndTime = DateTime.Now;
                        this.BeginInvoke((Action)delegate()
                        {
                            SongMaintenance_Tooltip_Label.Text = "總共匯入 " + Global.TotalList[0] + " 位最愛用戶及 " + Global.TotalList[1] + " 首最愛歌曲,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                            Common_SwitchSetUI(true);

                            Global.SongQueryFavoriteQuery = "False";
                            SongQuery_GetFavoriteUserList();
                            SongMaintenance_GetFavoriteUserList();
                            if (Global.SongQueryQueryType == "FavoriteQuery")
                            {
                                Global.SongQueryQueryType = "SongQuery";
                                SongQuery_EditMode_CheckBox.Enabled = true;
                                SongQuery_DataGridView.DataSource = null;
                                if (SongQuery_DataGridView.Columns.Count > 0) SongQuery_DataGridView.Columns.Remove("Song_FullPath");
                                SongQuery_QueryStatus_Label.Text = "";
                            }
                            SongMaintenance.DisposeSongDataTable();
                        });
                    });
                }
            }
            else
            {
                SongMaintenance_Tooltip_Label.Text = @"【SongMgr\Backup\Favorite.txt】我的最愛備份檔案不存在!";
            }
        }

        private void SongMaintenance_FavoriteImportTask()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;

            List<string> list = new List<string>();
            List<string> Addlist = new List<string>();

            OleDbConnection conn = new OleDbConnection();
            OleDbCommand Ucmd = new OleDbCommand();
            OleDbCommand Fcmd = new OleDbCommand();

            string TruncateSqlStr = "";

            conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
            TruncateSqlStr = "delete * from ktv_User";
            Ucmd = new OleDbCommand(TruncateSqlStr, conn);
            Ucmd.ExecuteNonQuery();

            TruncateSqlStr = "delete * from ktv_Favorite";
            Fcmd = new OleDbCommand(TruncateSqlStr, conn);
            Fcmd.ExecuteNonQuery();

            StreamReader sr = new StreamReader(Application.StartupPath + @"\SongMgr\Backup\Favorite.txt", Encoding.UTF8);
            while (!sr.EndOfStream)
            {
                Addlist.Add(sr.ReadLine());
            }
            sr.Close();

            string UserColumnStr = "User_Id, User_Name";
            string UserValuesStr = "@UserId, @UserName";
            string UserAddSqlStr = "insert into ktv_User ( " + UserColumnStr + " ) values ( " + UserValuesStr + " )";
            Ucmd = new OleDbCommand(UserAddSqlStr, conn);

            string FavoriteColumnStr = "User_Id, Song_Id";
            string FavoriteValuesStr = "@UserId, @SongId";
            string FavoriteAddSqlStr = "insert into ktv_Favorite ( " + FavoriteColumnStr + " ) values ( " + FavoriteValuesStr + " )";
            Fcmd = new OleDbCommand(FavoriteAddSqlStr, conn);

            foreach (string AddStr in Addlist)
            {
                list = new List<string>(Regex.Split(AddStr, ",", RegexOptions.None));
                switch (list[0])
                {
                    case "ktv_User":
                        Ucmd.Parameters.AddWithValue("@UserId", list[1]);
                        Ucmd.Parameters.AddWithValue("@UserName", list[2]);
                        Ucmd.ExecuteNonQuery();
                        Ucmd.Parameters.Clear();
                        lock (LockThis)
                        {
                            Global.TotalList[0]++;
                        }
                        break;
                    case "ktv_Favorite":
                        var query = from row in Global.SongDT.AsEnumerable()
                                    where row.Field<string>("Song_Lang").Equals(list[2]) &&
                                          row.Field<string>("Song_Singer").Equals(list[3]) &&
                                          row.Field<string>("Song_SongName").Equals(list[4])
                                    select row;

                        if (query.Count<DataRow>() > 0)
                        {
                            foreach (DataRow row in query)
                            {
                                string SongId = row["Song_Id"].ToString();
                                Fcmd.Parameters.AddWithValue("@UserId", list[1]);
                                Fcmd.Parameters.AddWithValue("@SongId", SongId);
                                Fcmd.ExecuteNonQuery();
                                Fcmd.Parameters.Clear();
                                lock (LockThis)
                                {
                                    Global.TotalList[1]++;
                                }
                                break;
                            }
                        }
                        break;
                }
            }
            conn.Close();
        }

        private void SongMaintenance_PlayCountReset_Button_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("你確定要重置播放次數嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Global.TimerStartTime = DateTime.Now;
                Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                SongMaintenance.CreateSongDataTable();
                Common_SwitchSetUI(false);

                var tasks = new List<Task>();
                tasks.Add(Task.Factory.StartNew(() => SongMaintenance_PlayCountResetTask()));

                Task.Factory.ContinueWhenAll(tasks.ToArray(), PlayCountResetEndTask =>
                {
                    Global.TimerEndTime = DateTime.Now;
                    this.BeginInvoke((Action)delegate()
                    {
                        SongMaintenance_Tooltip_Label.Text = "總共重置 " + Global.TotalList[0] + " 首歌曲的播放次數,失敗 " + Global.TotalList[1] + " 首,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                        Common_SwitchSetUI(true);
                    });
                    SongMaintenance.DisposeSongDataTable();
                });
            }
        }

        private void SongMaintenance_PlayCountResetTask()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            List<string> list = new List<string>();

            foreach (DataRow row in Global.SongDT.AsEnumerable())
            {
                list.Add("0" + "*" + row["Song_Id"].ToString());

                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在解析第 " + list.Count + " 首歌曲的播放次數資料,請稍待...";
                });
            }

            OleDbConnection conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
            OleDbCommand cmd = new OleDbCommand();
            string sqlColumnStr = "Song_PlayCount = @SongPlayCount";
            string SongUpdateSqlStr = "update ktv_Song set " + sqlColumnStr + " where Song_Id=@SongId";
            cmd = new OleDbCommand(SongUpdateSqlStr, conn);
            List<string> valuelist = new List<string>();

            foreach (string str in list)
            {
                valuelist = new List<string>(str.Split('*'));

                cmd.Parameters.AddWithValue("@SongPlayCount", valuelist[0]);
                cmd.Parameters.AddWithValue("@SongId", valuelist[1]);

                try
                {
                    cmd.ExecuteNonQuery();
                    lock (LockThis)
                    {
                        Global.TotalList[0]++;
                    }
                }
                catch
                {
                    lock (LockThis)
                    {
                        Global.TotalList[1]++;
                    }
                    Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【重置播放次數】更新資料庫時發生錯誤: " + str;
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                }
                cmd.Parameters.Clear();

                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在轉換第 " + Global.TotalList[0] + " 首歌曲的播放次數資料,請稍待...";
                });
            }
            conn.Close();
            list.Clear();
        }

        private void SongMaintenance_SongPathChange_Button_Click(object sender, EventArgs e)
        {
            if (SongMaintenance_SongPathChange_Button.Text == "瀏覽")
            {
                FolderBrowserDialog opd = new FolderBrowserDialog();
                if (SongMaintenance_DestSongPath_TextBox.Text != "") opd.SelectedPath = SongMaintenance_DestSongPath_TextBox.Text;

                if (opd.ShowDialog() == DialogResult.OK && opd.SelectedPath.Length > 0)
                {
                    SongMaintenance_DestSongPath_TextBox.Text = opd.SelectedPath + @"\";
                    SongMaintenance_SongPathChange_Button.Text = "變更";
                }
            }
            else
            {
                if (SongMaintenance_SrcSongPath_TextBox.Text == "")
                {
                    SongMaintenance_Tooltip_Label.Text = "你尚未輸入【原始路徑】!";
                }
                else
                {
                    if (MessageBox.Show("你確定要變更歌曲路徑嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        Global.TimerStartTime = DateTime.Now;
                        Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                        SongMaintenance.CreateSongDataTable();
                        Common_SwitchSetUI(false);

                        string SrcSongPath = SongMaintenance_SrcSongPath_TextBox.Text;
                        string DestSongPath = SongMaintenance_DestSongPath_TextBox.Text;

                        var tasks = new List<Task>();
                        tasks.Add(Task.Factory.StartNew(() => SongMaintenance_SongPathChangeTask(SrcSongPath, DestSongPath)));

                        Task.Factory.ContinueWhenAll(tasks.ToArray(), EndTask =>
                        {
                            Global.TimerEndTime = DateTime.Now;
                            this.BeginInvoke((Action)delegate()
                            {
                                SongMaintenance_Tooltip_Label.Text = "總共更新 " + Global.TotalList[0] + " 首歌曲的歌曲路徑資料,失敗 " + Global.TotalList[1] + " 首,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                                SongMaintenance_SrcSongPath_TextBox.Text = "";
                                SongMaintenance_DestSongPath_TextBox.Text = "";
                                SongMaintenance_SongPathChange_Button.Text = "瀏覽";
                                Common_SwitchSetUI(true);
                            });
                            SongMaintenance.DisposeSongDataTable();
                        });
                    }
                }
            }
        }

        private void SongMaintenance_SongPathChangeTask(object ObjSrcSongPath, object ObjDestSongPath)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            string SongPath = "";
            string SrcSongPath = (string)ObjSrcSongPath;
            string DestSongPath = (string)ObjDestSongPath;
            List<string> list = new List<string>();

            var query = from row in Global.SongDT.AsEnumerable()
                        where row.Field<string>("Song_Path").ToLower().Contains(SrcSongPath.ToLower())
                        select row;

            if (query.Count<DataRow>() > 0)
            {
                foreach (DataRow row in query)
                {
                    SongPath = row["Song_Path"].ToString().Replace(SrcSongPath, DestSongPath);
                    list.Add(SongPath + "*" + row["Song_Id"].ToString());

                    this.BeginInvoke((Action)delegate()
                    {
                        SongMaintenance_Tooltip_Label.Text = "正在解析第 " + list.Count + " 首歌曲的歌曲路徑資料,請稍待...";
                    });
                }
            }

            OleDbConnection conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
            OleDbCommand cmd = new OleDbCommand();
            string sqlColumnStr = "Song_Path = @SongPath";
            string SongUpdateSqlStr = "update ktv_Song set " + sqlColumnStr + " where Song_Id=@SongId";
            cmd = new OleDbCommand(SongUpdateSqlStr, conn);
            List<string> valuelist = new List<string>();

            foreach (string str in list)
            {
                valuelist = new List<string>(str.Split('*'));

                cmd.Parameters.AddWithValue("@SongPath", valuelist[0]);
                cmd.Parameters.AddWithValue("@SongId", valuelist[1]);

                try
                {
                    cmd.ExecuteNonQuery();
                    lock (LockThis)
                    {
                        Global.TotalList[0]++;
                    }
                }
                catch
                {
                    lock (LockThis)
                    {
                        Global.TotalList[1]++;
                    }
                    Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【變更歌曲路徑】更新資料庫時發生錯誤: " + str;
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                }
                cmd.Parameters.Clear();

                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在轉換第 " + Global.TotalList[0] + " 首歌曲的歌曲路徑資料,請稍待...";
                });
            }
            conn.Close();
            list.Clear();
        }

        private void SongMaintenance_SetCustomLangControl()
        {
            TextBox[] SongMaintenance_Lang_TextBox =
            {
                SongMaintenance_Lang1_TextBox,
                SongMaintenance_Lang2_TextBox,
                SongMaintenance_Lang3_TextBox,
                SongMaintenance_Lang4_TextBox,
                SongMaintenance_Lang5_TextBox,
                SongMaintenance_Lang6_TextBox,
                SongMaintenance_Lang7_TextBox,
                SongMaintenance_Lang8_TextBox,
                SongMaintenance_Lang9_TextBox,
                SongMaintenance_Lang10_TextBox
            };

            for (int i = 0; i < SongMaintenance_Lang_TextBox.Count<TextBox>(); i++)
            {
                SongMaintenance_Lang_TextBox[i].Text = Global.CrazyktvSongLangList[i];
            }

            TextBox[] SongMaintenance_LangIDStr_TextBox =
            {
              SongMaintenance_Lang1IDStr_TextBox,
              SongMaintenance_Lang2IDStr_TextBox,
              SongMaintenance_Lang3IDStr_TextBox,
              SongMaintenance_Lang4IDStr_TextBox,
              SongMaintenance_Lang5IDStr_TextBox,
              SongMaintenance_Lang6IDStr_TextBox,
              SongMaintenance_Lang7IDStr_TextBox,
              SongMaintenance_Lang8IDStr_TextBox,
              SongMaintenance_Lang9IDStr_TextBox,
              SongMaintenance_Lang10IDStr_TextBox
            };

            for (int i = 0; i < SongMaintenance_LangIDStr_TextBox.Count<TextBox>(); i++)
            {
                SongMaintenance_LangIDStr_TextBox[i].Text = Global.CrazyktvSongLangIDList[i];
            }
        }

        private void SongMaintenance_TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (SongMaintenance_TabControl.SelectedTab.Name)
            {
                case "SongMaintenance_CustomLang_TabPage":
                    SongMaintenance_Save_Button.Enabled = true;
                    break;
                default:
                    SongMaintenance_Save_Button.Enabled = false;
                    break;
            }
        }

        private void SongMaintenance_Save_Button_Click(object sender, EventArgs e)
        {
            Global.CrazyktvSongLangList = new List<string>();
            Global.CrazyktvSongLangIDList = new List<string>();
            TextBox[] SongMaintenance_Lang_TextBox =
            {
                SongMaintenance_Lang1_TextBox,
                SongMaintenance_Lang2_TextBox,
                SongMaintenance_Lang3_TextBox,
                SongMaintenance_Lang4_TextBox,
                SongMaintenance_Lang5_TextBox,
                SongMaintenance_Lang6_TextBox,
                SongMaintenance_Lang7_TextBox,
                SongMaintenance_Lang8_TextBox,
                SongMaintenance_Lang9_TextBox,
                SongMaintenance_Lang10_TextBox
            };

            for (int i = 0; i < SongMaintenance_Lang_TextBox.Count<TextBox>(); i++)
            {
                Global.CrazyktvSongLangList.Add(SongMaintenance_Lang_TextBox[i].Text);
            }
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "CrazyktvSongLangStr", string.Join(",", Global.CrazyktvSongLangList));

            TextBox[] SongMaintenance_LangIDStr_TextBox =
            {
              SongMaintenance_Lang1IDStr_TextBox,
              SongMaintenance_Lang2IDStr_TextBox,
              SongMaintenance_Lang3IDStr_TextBox,
              SongMaintenance_Lang4IDStr_TextBox,
              SongMaintenance_Lang5IDStr_TextBox,
              SongMaintenance_Lang6IDStr_TextBox,
              SongMaintenance_Lang7IDStr_TextBox,
              SongMaintenance_Lang8IDStr_TextBox,
              SongMaintenance_Lang9IDStr_TextBox,
              SongMaintenance_Lang10IDStr_TextBox
            };

            for (int i = 0; i < SongMaintenance_LangIDStr_TextBox.Count<TextBox>(); i++)
            {
                Global.CrazyktvSongLangIDList.Add(SongMaintenance_LangIDStr_TextBox[i].Text);
            }
            CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "CrazyktvSongLangIDStr", string.Join("*", Global.CrazyktvSongLangIDList));

            Global.TimerStartTime = DateTime.Now;
            Global.TotalList = new List<int>() { 0, 0, 0, 0 };
            Common_SwitchSetUI(false);

            var tasks = new List<Task>();
            tasks.Add(Task.Factory.StartNew(() => SongMaintenance_SongLangUpdateTask()));

            Task.Factory.ContinueWhenAll(tasks.ToArray(), EndTask =>
            {
                Global.TimerEndTime = DateTime.Now;
                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "總共更新 " + Global.TotalList[0] + " 筆自訂語系資料,失敗 " + Global.TotalList[1] + " 筆,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                    Common_RefreshSongLang();
                    Common_SwitchSetUI(true);
                });
            });
        }

        private void SongMaintenance_SongLangUpdateTask()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            List<string> list = new List<string>();

            if (File.Exists(Application.StartupPath + @"\Lang\Taiwan.lang"))
            {
                StreamReader sr = new StreamReader(Application.StartupPath + @"\Lang\Taiwan.lang", Encoding.Unicode);
                while (!sr.EndOfStream)
                {
                    list.Add(sr.ReadLine());
                }
                sr.Close();

                list[2] = string.Join(",", Global.CrazyktvSongLangList);

                StreamWriter sw = new StreamWriter(Application.StartupPath + @"\Lang\Taiwan.lang", false, Encoding.Unicode);
                foreach (string str in list)
                {
                    sw.WriteLine(str);
                }
                sw.Close();

                CommonFunc.SaveConfigXmlFile(Global.CrazyktvCfgFile, "Language", "Taiwan");
            }

            OleDbConnection conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
            OleDbCommand cmd = new OleDbCommand();
            string sqlColumnStr = "Langauage_Name = @LangauageName";
            string SongUpdateSqlStr = "update ktv_Langauage set " + sqlColumnStr + " where Langauage_Id=@LangauageId";
            cmd = new OleDbCommand(SongUpdateSqlStr, conn);

            foreach (string str in Global.CrazyktvSongLangList)
            {
                cmd.Parameters.AddWithValue("@LangauageName", str);
                cmd.Parameters.AddWithValue("@LangauageId", Global.CrazyktvSongLangList.IndexOf(str));

                try
                {
                    cmd.ExecuteNonQuery();
                    lock (LockThis)
                    {
                        Global.TotalList[0]++;
                    }
                }
                catch
                {
                    lock (LockThis)
                    {
                        Global.TotalList[1]++;
                    }
                    Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【自訂語系】更新資料庫時發生錯誤: " + str;
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                }
                cmd.Parameters.Clear();

                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在更新第 " + Global.TotalList[0] + " 筆自訂語系資料,請稍待...";
                });
            }
            conn.Close();
        }

        private void SongMaintenance_SongWordCountCorrect_Button_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("你確定要校正歌曲字數嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Global.TimerStartTime = DateTime.Now;
                Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                SongMaintenance.CreateSongDataTable();
                Common_SwitchSetUI(false);

                var tasks = new List<Task>();
                tasks.Add(Task.Factory.StartNew(() => SongMaintenance_WordCountCorrectTask()));

                Task.Factory.ContinueWhenAll(tasks.ToArray(), EndTask =>
                {
                    Global.TimerEndTime = DateTime.Now;
                    this.BeginInvoke((Action)delegate()
                    {
                        SongMaintenance_Tooltip_Label.Text = "總共更新 " + Global.TotalList[0] + " 首歌曲的歌曲字數,失敗 " + Global.TotalList[1] + " 首,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                        Common_SwitchSetUI(true);
                    });
                    SongMaintenance.DisposeSongDataTable();
                });
            }
        }

        private void SongMaintenance_WordCountCorrectTask()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            List<string> list = new List<string>();
            List<string> SongWordCountList = new List<string>();

            foreach (DataRow row in Global.SongDT.AsEnumerable())
            {
                SongWordCountList = CommonFunc.GetSongWordCount(row["Song_SongName"].ToString());
                string SongWordCount = SongWordCountList[0];

                list.Add(SongWordCount + "*" + row["Song_Id"].ToString());

                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在解析第 " + list.Count + " 首歌曲的字數資料,請稍待...";
                });
            }

            OleDbConnection conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
            OleDbCommand cmd = new OleDbCommand();
            string sqlColumnStr = "Song_WordCount = @SongWordCount";
            string SongUpdateSqlStr = "update ktv_Song set " + sqlColumnStr + " where Song_Id=@SongId";
            cmd = new OleDbCommand(SongUpdateSqlStr, conn);
            List<string> valuelist = new List<string>();

            foreach (string str in list)
            {
                valuelist = new List<string>(str.Split('*'));

                cmd.Parameters.AddWithValue("@SongWordCount", valuelist[0]);
                cmd.Parameters.AddWithValue("@SongId", valuelist[1]);

                try
                {
                    cmd.ExecuteNonQuery();
                    lock (LockThis)
                    {
                        Global.TotalList[0]++;
                    }
                }
                catch
                {
                    lock (LockThis)
                    {
                        Global.TotalList[1]++;
                    }
                    Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【字數校正】更新資料庫時發生錯誤: " + str;
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                }
                cmd.Parameters.Clear();

                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在轉換第 " + Global.TotalList[0] + " 首歌曲的字數資料,請稍待...";
                });
            }
            conn.Close();
            list.Clear();
        }

        private void SongMaintenance_RemoteCfgExport_Button_Click(object sender, EventArgs e)
        {
            List<string> list = new List<string>();
            string RemoteQuerySqlStr = "";
            DataTable dt = new DataTable();

            string sqlColumnStr = "Remote_Id, Remote_Subject, Remote_Controler, Remote_Controler2, Remote_Name";
            RemoteQuerySqlStr = "select " + sqlColumnStr + " from ktv_Remote";
            dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, RemoteQuerySqlStr, "");

            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.AsEnumerable())
                {
                    list.Add("ktv_Remote," + row["Remote_Id"].ToString() + "," + row["Remote_Subject"].ToString() + "," + row["Remote_Controler"].ToString() + "," + row["Remote_Controler2"].ToString() + "," + row["Remote_Name"].ToString());
                }
            }

            if (!Directory.Exists(Application.StartupPath + @"\SongMgr\Backup")) Directory.CreateDirectory(Application.StartupPath + @"\SongMgr\Backup");
            StreamWriter sw = new StreamWriter(Application.StartupPath + @"\SongMgr\Backup\Remote.txt");
            foreach (string str in list)
            {
                sw.WriteLine(str);
            }

            SongMaintenance_Tooltip_Label.Text = @"已將遙控設定匯出至【SongMgr\Backup\Remote.txt】檔案。";
            sw.Close();
            list.Clear();
            dt.Dispose();
        }

        private void SongMaintenance_RemoteCfgImport_Button_Click(object sender, EventArgs e)
        {
            if (File.Exists(Application.StartupPath + @"\SongMgr\Backup\Remote.txt"))
            {
                if (SongMaintenance_Tooltip_Label.Text == @"【SongMgr\Backup\Remote.txt】遙控設定備份檔案不存在!") SongMaintenance_Tooltip_Label.Text = "";
                if (MessageBox.Show("你確定要重置並匯入遙控設定嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Global.TimerStartTime = DateTime.Now;
                    Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                    Common_SwitchSetUI(false);

                    SongMaintenance_Tooltip_Label.Text = "正在匯入遙控設定,請稍待...";

                    var tasks = new List<Task>();
                    tasks.Add(Task.Factory.StartNew(() => SongMaintenance_RemoteCfgImportTask()));

                    Task.Factory.ContinueWhenAll(tasks.ToArray(), EndTask =>
                    {
                        Global.TimerEndTime = DateTime.Now;
                        this.BeginInvoke((Action)delegate()
                        {
                            SongMaintenance_Tooltip_Label.Text = "總共匯入 " + Global.TotalList[0] + " 項遙控設定,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                            Common_SwitchSetUI(true);
                        });
                    });
                }
            }
            else
            {
                SongMaintenance_Tooltip_Label.Text = @"【SongMgr\Backup\Remote.txt】遙控設定備份檔案不存在!";
            }
        }

        private void SongMaintenance_RemoteCfgImportTask()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            List<string> list = new List<string>();
            List<string> Addlist = new List<string>();

            OleDbConnection conn = new OleDbConnection();
            OleDbCommand cmd = new OleDbCommand();
            OleDbCommand Versioncmd = new OleDbCommand();

            conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
            string TruncateSqlStr = "delete * from ktv_Remote";
            cmd = new OleDbCommand(TruncateSqlStr, conn);
            cmd.ExecuteNonQuery();
            
            StreamReader sr = new StreamReader(Application.StartupPath + @"\SongMgr\Backup\Remote.txt", Encoding.UTF8);;

            while (!sr.EndOfStream)
            {
                Addlist.Add(sr.ReadLine());
            }
            sr.Close();

            string sqlColumnStr = "Remote_Id, Remote_Subject, Remote_Controler, Remote_Controler2, Remote_Name";
            string sqlValuesStr = "@RemoteId, @RemoteSubject, @RemoteControler, @RemoteControler2, @RemoteName";
            string RemoteAddSqlStr = "insert into ktv_Remote ( " + sqlColumnStr + " ) values ( " + sqlValuesStr + " )";
            cmd = new OleDbCommand(RemoteAddSqlStr, conn);

            foreach (string AddStr in Addlist)
            {
                list = new List<string>(Regex.Split(AddStr, ",", RegexOptions.None));
                switch (list[0])
                {
                    case "ktv_Remote":
                        cmd.Parameters.AddWithValue("@RemoteId", list[1]);
                        cmd.Parameters.AddWithValue("@RemoteSubject", list[2]);
                        cmd.Parameters.AddWithValue("@RemoteControler", list[3]);
                        cmd.Parameters.AddWithValue("@RemoteControler2", list[4]);
                        cmd.Parameters.AddWithValue("@RemoteName", list[5]);

                        cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                        lock (LockThis) { Global.TotalList[0]++; }
                        break;
                }
                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在匯入第 " + Global.TotalList[0] + " 項遙控設定資料,請稍待...";
                });
            }
            conn.Close();
        }

        private void SongMaintenance_PhoneticsExport_Button_Click(object sender, EventArgs e)
        {
            List<string> list = new List<string>();
            string PhoneticsQuerySqlStr = "";
            DataTable dt = new DataTable();

            string sqlColumnStr = "Word, Code, Spell, PenStyle, SortIdx, Strokes";
            PhoneticsQuerySqlStr = "select " + sqlColumnStr + " from ktv_Phonetics order by Code, SortIdx";
            dt = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, PhoneticsQuerySqlStr, "");
            
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.AsEnumerable())
                {
                    //list.Add("ktv_Phonetics," + row["Word"].ToString() + "," + CommonFunc.GetWordUnicode(row["Word"].ToString()) + "," + row["Spell"].ToString() + "," + row["PenStyle"].ToString() + "," + row["SortIdx"].ToString() + "," + row["Strokes"].ToString());
                    list.Add("ktv_Phonetics," + row["Word"].ToString() + "," + row["Code"].ToString() + "," + row["Spell"].ToString() + "," + row["PenStyle"].ToString() + "," + row["SortIdx"].ToString() + "," + row["Strokes"].ToString());
                }
            }

            if (!Directory.Exists(Application.StartupPath + @"\SongMgr\Backup")) Directory.CreateDirectory(Application.StartupPath + @"\SongMgr\Backup");
            StreamWriter sw = new StreamWriter(Application.StartupPath + @"\SongMgr\Backup\Phonetics.txt");
            foreach (string str in list)
            {
                sw.WriteLine(str);
            }

            SongMaintenance_Tooltip_Label.Text = @"已將拼音資料匯出至【SongMgr\Backup\Phonetics.txt】檔案。";
            sw.Close();
            list.Clear();
            dt.Dispose();
        }

        private void SongMaintenance_PhoneticsImport_Button_Click(object sender, EventArgs e)
        {
            if (File.Exists(Application.StartupPath + @"\SongMgr\Backup\Phonetics.txt"))
            {
                if (SongMaintenance_Tooltip_Label.Text == @"【SongMgr\Backup\Phonetics.txt】拼音資料備份檔案不存在!") SongMaintenance_Tooltip_Label.Text = "";
                if (MessageBox.Show("你確定要重置並匯入拼音資料嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Global.TimerStartTime = DateTime.Now;
                    Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                    Common_SwitchSetUI(false);

                    SongMaintenance_Tooltip_Label.Text = "正在匯入拼音資料,請稍待...";

                    var tasks = new List<Task>();
                    tasks.Add(Task.Factory.StartNew(() => SongMaintenance_PhoneticsImportTask(false)));

                    Task.Factory.ContinueWhenAll(tasks.ToArray(), EndTask =>
                    {
                        Global.TimerEndTime = DateTime.Now;
                        this.BeginInvoke((Action)delegate()
                        {
                            SongMaintenance_Tooltip_Label.Text = "總共匯入 " + Global.TotalList[0] + " 項拼音資料,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                            Common_SwitchSetUI(true);
                        });
                    });
                }
            }
            else
            {
                SongMaintenance_Tooltip_Label.Text = @"【SongMgr\Backup\Phonetics.txt】拼音資料備份檔案不存在!";
            }
        }

        private void SongMaintenance_PhoneticsImportTask(bool Update)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            List<string> list = new List<string>();
            List<string> Addlist = new List<string>();

            OleDbConnection conn = new OleDbConnection();
            OleDbCommand cmd = new OleDbCommand();
            OleDbCommand Versioncmd = new OleDbCommand();

            conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
            string TruncateSqlStr = "delete * from ktv_Phonetics";
            cmd = new OleDbCommand(TruncateSqlStr, conn);
            cmd.ExecuteNonQuery();

            StreamReader sr;
            if (Update) 
            {
                sr = new StreamReader(Application.StartupPath + @"\SongMgr\Update\UpdatePhoneticsDB.txt", Encoding.UTF8);
            }
            else
            {
                sr = new StreamReader(Application.StartupPath + @"\SongMgr\Backup\Phonetics.txt", Encoding.UTF8);
            }
            
            while (!sr.EndOfStream)
            {
                Addlist.Add(sr.ReadLine());
            }
            sr.Close();

            string sqlColumnStr = "Word, Code, Spell, PenStyle, SortIdx, Strokes";
            string sqlValuesStr = "@Word, @Code, @Spell, @PenStyle, @SortIdx, @Strokes";
            string RemoteAddSqlStr = "insert into ktv_Phonetics ( " + sqlColumnStr + " ) values ( " + sqlValuesStr + " )";
            cmd = new OleDbCommand(RemoteAddSqlStr, conn);

            foreach (string AddStr in Addlist)
            {
                list = new List<string>(Regex.Split(AddStr, ",", RegexOptions.None));
                switch (list[0])
                {
                    case "ktv_Version":
                        string VersionSqlStr = "PhoneticsDB = @PhoneticsDB";
                        string VersionUpdateSqlStr = "update ktv_Version set " + VersionSqlStr + " where Id=@Id";
                        Versioncmd = new OleDbCommand(VersionUpdateSqlStr, conn);
                        Versioncmd.Parameters.AddWithValue("@PhoneticsDB", list[1]);
                        Versioncmd.Parameters.AddWithValue("@Id", "1");
                        Versioncmd.ExecuteNonQuery();
                        Versioncmd.Parameters.Clear();
                        break;
                    case "ktv_Phonetics":
                        cmd.Parameters.AddWithValue("@Word", list[1]);
                        cmd.Parameters.AddWithValue("@Code", list[2]);
                        cmd.Parameters.AddWithValue("@Spell", list[3]);
                        cmd.Parameters.AddWithValue("@PenStyle", list[4]);
                        cmd.Parameters.AddWithValue("@SortIdx", list[5]);
                        cmd.Parameters.AddWithValue("@Strokes", list[6]);
                        cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                        lock (LockThis) { Global.TotalList[0]++; }
                        break;
                }

                this.BeginInvoke((Action)delegate()
                {
                        
                    SongMaintenance_Tooltip_Label.Text = "正在匯入第 " + Global.TotalList[0] + " 項拼音資料,請稍待...";
                });
            }
            conn.Close();
        }

        private void SongMaintenance_NonPhoneticsWordLog_Button_Click(object sender, EventArgs e)
        {
            Global.TimerStartTime = DateTime.Now;
            Global.TotalList = new List<int>() { 0, 0, 0, 0 };
            SongMaintenance.CreateSongDataTable();
            Common_SwitchSetUI(false);

            var tasks = new List<Task>();
            tasks.Add(Task.Factory.StartNew(() => SongMaintenance_NonPhoneticsWordLogTask()));

            Task.Factory.ContinueWhenAll(tasks.ToArray(), EndTask =>
            {
                Global.TimerEndTime = DateTime.Now;
                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "總共從 " + Global.TotalList[0] + " 首歌曲,查詢到 "  + Global.TotalList[1] + " 筆無拼音資料的文字,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                    Common_SwitchSetUI(true);
                });
                SongMaintenance.DisposeSongDataTable();
            });
        }

        private void SongMaintenance_NonPhoneticsWordLogTask()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            List<string> list = new List<string>();
            List<string> wordlist = new List<string>();
            string MatchWord = "";

            Parallel.ForEach(Global.SongDT.AsEnumerable(), (row, loopState) =>
            {
                Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
                MatchWord = row["Song_Singer"].ToString() + row["Song_SongName"].ToString();
                MatchWord = Regex.Replace(MatchWord, @"[\{\(\[｛（［【].+?[】］）｝\]\)\}]", "");

                if (MatchWord != "")
                {
                    MatchCollection CJKCharMatches = Regex.Matches(MatchWord, @"([\u2E80-\u33FF]|[\u4E00-\u9FCC\u3400-\u4DB5\uFA0E\uFA0F\uFA11\uFA13\uFA14\uFA1F\uFA21\uFA23\uFA24\uFA27-\uFA29]|[\ud840-\ud868][\udc00-\udfff]|\ud869[\udc00-\uded6\udf00-\udfff]|[\ud86a-\ud86c][\udc00-\udfff]|\ud86d[\udc00-\udf34\udf40-\udfff]|\ud86e[\udc00-\udc1d]|[\uac00-\ud7ff])");
                    if (CJKCharMatches.Count > 0)
                    {
                        foreach (Match m in CJKCharMatches)
                        {
                            if (wordlist.IndexOf(m.Value) < 0)
                            {
                                // 查找資料庫拼音資料
                                var query = from prow in Global.PhoneticsDT.AsEnumerable()
                                            where prow.Field<string>("Word").Equals(m.Value) & prow.Field<Int16>("SortIdx") < 2
                                            select prow;

                                if (query.Count<DataRow>() == 0)
                                {
                                    if (list.IndexOf(m.Value) < 0)
                                    {
                                        list.Add(m.Value);
                                        lock (LockThis) { Global.TotalList[1]++; }
                                    }
                                }
                                wordlist.Add(m.Value);
                            }
                        }
                    }
                }
                lock (LockThis) { Global.TotalList[0]++; }
                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在解析第 " + Global.TotalList[0] + " 首歌曲的拼音資料,請稍待...";
                });
            });

            wordlist.Clear();
            if (list.Count > 0)
            {
                Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【記錄無拼音字】以下為無拼音資料的文字: " + string.Join(",", list);
                Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
            }
        }

        private void SongMaintenance_RebuildSongStructure_Button_Click(object sender, EventArgs e)
        {
            if (SongMaintenance_RebuildSongStructure_Button.Text == "瀏覽")
            {
                FolderBrowserDialog opd = new FolderBrowserDialog();
                if (SongMaintenance_RebuildSongStructure_TextBox.Text != "") opd.SelectedPath = SongMaintenance_RebuildSongStructure_TextBox.Text;

                if (opd.ShowDialog() == DialogResult.OK && opd.SelectedPath.Length > 0)
                {
                    if (Directory.GetFiles(opd.SelectedPath, "*", SearchOption.AllDirectories).Count() == 0)
                    {
                        if (SongMaintenance_Tooltip_Label.Text == "請選擇一個空白的資料夾!") SongMaintenance_Tooltip_Label.Text = "";
                        SongMaintenance_RebuildSongStructure_TextBox.Text = opd.SelectedPath;
                        SongMaintenance_RebuildSongStructure_Button.Text = "重建";
                    }
                    else
                    {
                        SongMaintenance_Tooltip_Label.Text = "請選擇一個空白的資料夾!";
                    }
                }
            }
            else
            {
                if (SongMaintenance_RebuildSongStructure_TextBox.Text == "")
                {
                    SongMaintenance_Tooltip_Label.Text = "你尚未選擇【重建資料夾】!";
                }
                else
                {
                    if (MessageBox.Show("你確定要重建歌庫結構嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        Global.TimerStartTime = DateTime.Now;
                        Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                        SongMaintenance.CreateSongDataTable();
                        Common_SwitchSetUI(false);

                        string RebuildSongPath = SongMaintenance_RebuildSongStructure_TextBox.Text;
                        
                        var tasks = new List<Task>();
                        tasks.Add(Task.Factory.StartNew(() => SongMaintenance_RebuildSongStructureTask(RebuildSongPath, Global.SongMgrSongAddMode)));

                        Task.Factory.ContinueWhenAll(tasks.ToArray(), EndTask =>
                        {
                            Global.TimerEndTime = DateTime.Now;
                            this.BeginInvoke((Action)delegate()
                            {
                                SongMaintenance_Tooltip_Label.Text = "總共重建 " + Global.TotalList[0] + " 首歌曲,忽略 " + Global.TotalList[1] + " 首,失敗 " + Global.TotalList[2] + " 首,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成重建。";
                                SongMaintenance_RebuildSongStructure_TextBox.Text = "";
                                SongMaintenance_RebuildSongStructure_Button.Text = "瀏覽";

                                Global.SongMgrDestFolder = RebuildSongPath;
                                SongMgrCfg_DestFolder_TextBox.Text = RebuildSongPath;
                                CommonFunc.SaveConfigXmlFile(Global.SongMgrCfgFile, "SongMgrDestFolder", Global.SongMgrDestFolder);

                                // 統計歌曲數量
                                Task.Factory.StartNew(() => Common_GetSongStatisticsTask());
                                Common_SwitchSetUI(true);
                            });
                            SongMaintenance.DisposeSongDataTable();
                        });
                    }
                }
            }
        }

        private void SongMaintenance_RebuildSongStructureTask(string RebuildSongPath, string RebuildMode)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            Global.TotalList = new List<int>() { 0, 0, 0, 0 };
            List<string> RebuildSongFileValueList = new List<string>();

            foreach (DataRow row in Global.SongDT.Rows)
            {
                string SongId = row["Song_Id"].ToString();
                string SongLang = row["Song_Lang"].ToString();
                int SongSingerType = Convert.ToInt32(row["Song_SingerType"]);
                string SongSinger = row["Song_Singer"].ToString();
                string SongSongName = row["Song_SongName"].ToString();
                int SongTrack = Convert.ToInt32(row["Song_Track"]);
                string SongSongType = row["Song_SongType"].ToString();
                string SongFileName = row["Song_FileName"].ToString();
                string SongPath = row["Song_Path"].ToString();

                if (SongSingerType < 0 | SongSingerType > 10)
                {
                    SongSingerType = 10;
                    Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【歌庫結構重建】此首歌曲歌手類別數值錯誤,已自動將其數值改為10: " + SongId + "*" + SongSongName;
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                }

                string SongSingerStr = SongSinger;
                string SingerTypeStr = CommonFunc.GetSingerTypeStr(SongSingerType, 2, "null");
                string CrtchorusSeparate;
                string SongInfoSeparate;
                if (Global.SongMgrChorusSeparate == "1") { CrtchorusSeparate = "&"; } else { CrtchorusSeparate = "+"; }
                if (Global.SongMgrSongInfoSeparate == "1") { SongInfoSeparate = "_"; } else { SongInfoSeparate = "-"; }

                if (SongTrack < 1 | SongTrack > 5 )
                {
                    SongTrack = 1;
                    Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【歌庫結構重建】此首歌曲聲道數值錯誤,已自動將其數值改為1: " + SongId + "*" + SongSongName;
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                }
                string SongTrackStr = CommonFunc.GetSongTrackStr(SongTrack - 1, 1, "null");

                // 重建歌檔
                string SongSrcPath = Path.Combine(SongPath, SongFileName);
                string SongExtension = Path.GetExtension(SongSrcPath);

                if (SongSingerType == 3)
                {
                    SongSingerStr = Regex.Replace(SongSinger, "[&+]", CrtchorusSeparate, RegexOptions.IgnoreCase);
                }

                switch (Global.SongMgrFolderStructure)
                {
                    case "1":
                        if (Global.SongMgrChorusMerge == "True" & SongSingerType == 3)
                        {
                            SongPath = RebuildSongPath + @"\" + SongLang + @"\" + SingerTypeStr + @"\";
                        }
                        else
                        {
                            SongPath = RebuildSongPath + @"\" + SongLang + @"\" + SingerTypeStr + @"\" + SongSingerStr + @"\";
                        }
                        break;
                    case "2":
                        SongPath = RebuildSongPath + @"\" + SongLang + @"\" + SingerTypeStr + @"\";
                        break;
                }

                switch (Global.SongMgrFileStructure)
                {
                    case "1":
                        if (SongSongType == "")
                        {
                            SongFileName = SongSingerStr + SongInfoSeparate + SongSongName + SongInfoSeparate + SongTrackStr + SongExtension;
                        }
                        else
                        {
                            SongFileName = SongSingerStr + SongInfoSeparate + SongSongName + SongInfoSeparate + SongSongType + SongInfoSeparate + SongTrackStr + SongExtension;
                        }
                        break;
                    case "2":
                        if (SongSongType == "")
                        {
                            SongFileName = SongSongName + SongInfoSeparate + SongSingerStr + SongInfoSeparate + SongTrackStr + SongExtension;
                        }
                        else
                        {
                            SongFileName = SongSongName + SongInfoSeparate + SongSingerStr + SongInfoSeparate + SongSongType + SongInfoSeparate + SongTrackStr + SongExtension;
                        }
                        break;
                    case "3":
                        if (SongSongType == "")
                        {
                            SongFileName = SongId + SongInfoSeparate + SongSingerStr + SongInfoSeparate + SongSongName + SongInfoSeparate + SongTrackStr + SongExtension;
                        }
                        else
                        {
                            SongFileName = SongId + SongInfoSeparate + SongSingerStr + SongInfoSeparate + SongSongName + SongInfoSeparate + SongSongType + SongInfoSeparate + SongTrackStr + SongExtension;
                        }
                        break;
                }

                string SongDestPath = Path.Combine(SongPath, SongFileName);
                bool FileIOError = false;

                if (File.Exists(SongSrcPath))
                {
                    if (!Directory.Exists(SongPath)) Directory.CreateDirectory(SongPath);
                   
                    try
                    {
                        switch (RebuildMode)
                        {
                            case "1":
                                if (File.Exists(SongDestPath)) File.Delete(SongDestPath);
                                File.Move(SongSrcPath, SongDestPath);
                                break;
                            case "2":
                                File.Copy(SongSrcPath, SongDestPath, true);
                                break;
                        }
                    }
                    catch
                    {
                        FileIOError = true;
                        Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                        Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【歌庫結構重建】檔案處理發生錯誤: " + SongSrcPath + " (唯讀或使用中)";
                        Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                        lock (LockThis) { Global.TotalList[2]++; }
                    }
                }
                else
                {
                    FileIOError = true;
                    Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【歌庫結構重建】此首歌曲檔案不存在,已自動忽略重建檔案: " + SongId + "*" + SongSrcPath;
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                    lock (LockThis) { Global.TotalList[1]++; }
                }

                if (!FileIOError)
                {
                    string RebuildSongFileValue = SongId + "*" + SongSingerType + "*" + SongTrack + "*" + SongFileName + "*" + SongPath;
                    RebuildSongFileValueList.Add(RebuildSongFileValue);
                    lock (LockThis) { Global.TotalList[0]++; }

                    this.BeginInvoke((Action)delegate()
                    {
                        SongMaintenance_Tooltip_Label.Text = "已成功將 " + Global.TotalList[0] + " 首歌曲重建至重建資料夾,請稍待...";
                    });
                }
            }

            OleDbConnection conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
            OleDbCommand cmd = new OleDbCommand();
            string sqlColumnStr = "Song_Id = @SongId, Song_SingerType = @SongSingerType, Song_Track = @SongTrack, Song_FileName = @SongFileName, Song_Path = @SongPath";
            string SongUpdateSqlStr = "update ktv_Song set " + sqlColumnStr + " where Song_Id=@SongId";
            cmd = new OleDbCommand(SongUpdateSqlStr, conn);

            List<string> valuelist = new List<string>();

            foreach (string str in RebuildSongFileValueList)
            {
                valuelist = new List<string>(str.Split('*'));

                cmd.Parameters.AddWithValue("@SongId", valuelist[0]);
                cmd.Parameters.AddWithValue("@SongSingerType", valuelist[1]);
                cmd.Parameters.AddWithValue("@SongTrack", valuelist[2]);
                cmd.Parameters.AddWithValue("@SongFileName", valuelist[3]);
                cmd.Parameters.AddWithValue("@SongPath", valuelist[4]);
                cmd.Parameters.AddWithValue("@SongId", valuelist[0]);

                try
                {
                    cmd.ExecuteNonQuery();
                    lock (LockThis) { Global.TotalList[3]++; }
                }
                catch
                {
                    Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【歌庫轉換】寫入重建檔案路徑至資料庫時發生錯誤: " + valuelist[0] + "*" + valuelist[3] + "*" + valuelist[4];
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                    lock (LockThis)
                    {
                        Global.TotalList[0]--;
                        Global.TotalList[2]++;
                    }
                }
                cmd.Parameters.Clear();

                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在更新第 " + Global.TotalList[3] + " 首歌曲的資料庫資料,請稍待...";
                });
            }
            RebuildSongFileValueList.Clear();
            conn.Close();
        }

        private void SongMaintenance_SingerImportTask()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            List<string> list = new List<string>();
            List<string> Addlist = new List<string>();

            OleDbConnection conn = new OleDbConnection();
            OleDbCommand Versioncmd = new OleDbCommand();
            OleDbCommand allsingercmd = new OleDbCommand();

            conn = CommonFunc.OleDbOpenConn(Global.CrazyktvDatabaseFile, "");
            string TruncateSqlStr = "delete * from ktv_AllSinger";
            allsingercmd = new OleDbCommand(TruncateSqlStr, conn);
            allsingercmd.ExecuteNonQuery();

            StreamReader sr = new StreamReader(Application.StartupPath + @"\SongMgr\Update\UpdateSingerDB.txt", Encoding.UTF8);
            while (!sr.EndOfStream)
            {
                Addlist.Add(sr.ReadLine());
            }
            sr.Close();

            string sqlColumnStr = "Singer_Id, Singer_Name, Singer_Type, Singer_Spell, Singer_Strokes, Singer_SpellNum, Singer_PenStyle";
            string sqlValuesStr = "@SingerId, @SingerName, @SingerType, @SingerSpell, @SingerStrokes, @SingerSpellNum, @SingerPenStyle";
            string AllSingerAddSqlStr = "insert into ktv_AllSinger ( " + sqlColumnStr + " ) values ( " + sqlValuesStr + " )";
            allsingercmd = new OleDbCommand(AllSingerAddSqlStr, conn);

            foreach (string AddStr in Addlist)
            {
                list = new List<string>(Regex.Split(AddStr, ",", RegexOptions.None));
                switch (list[0])
                {
                    case "ktv_Version":
                        string VersionSqlStr = "SingerDB = @SingerDB";
                        string VersionUpdateSqlStr = "update ktv_Version set " + VersionSqlStr + " where Id=@Id";
                        Versioncmd = new OleDbCommand(VersionUpdateSqlStr, conn);

                        Versioncmd.Parameters.AddWithValue("@SingerDB", list[1]);
                        Versioncmd.Parameters.AddWithValue("@Id", "1");
                        Versioncmd.ExecuteNonQuery();
                        Versioncmd.Parameters.Clear();
                        break;
                    case "ktv_AllSinger":
                        allsingercmd.Parameters.AddWithValue("@SingerId", list[1]);
                        allsingercmd.Parameters.AddWithValue("@SingerName", list[2]);
                        allsingercmd.Parameters.AddWithValue("@SingerType", list[3]);
                        allsingercmd.Parameters.AddWithValue("@SingerSpell", list[4]);
                        allsingercmd.Parameters.AddWithValue("@SingerStrokes", list[5]);
                        allsingercmd.Parameters.AddWithValue("@SingerSpellNum", list[6]);
                        allsingercmd.Parameters.AddWithValue("@SingerPenStyle", list[7]);

                        allsingercmd.ExecuteNonQuery();
                        allsingercmd.Parameters.Clear();
                        lock (LockThis) { Global.TotalList[0]++; }
                        break;
                }
                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "正在更新第 " + Global.TotalList[0] + " 位歌手資料,請稍待...";
                });
            }
            conn.Close();
        }

        private void SongMaintenance_CompactAccessDB_Button_Click(object sender, EventArgs e)
        {
            if (File.Exists(Global.CrazyktvDatabaseFile))
            {
                if (MessageBox.Show("你確定要壓縮並修復資料庫嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (!Directory.Exists(Application.StartupPath + @"\SongMgr\Backup")) Directory.CreateDirectory(Application.StartupPath + @"\SongMgr\Backup");
                    File.Copy(Global.CrazyktvDatabaseFile, Application.StartupPath + @"\SongMgr\Backup\" + DateTime.Now.ToLongDateString() + "_Compact_CrazySong.mdb", true);

                    Common_SwitchSetUI(false);
                    CommonFunc.CompactAccessDB("Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + Global.CrazyktvDatabaseFile + ";", Global.CrazyktvDatabaseFile);
                    Common_SwitchSetUI(true);
                }
            }
        }

        private void SongMaintenance_RemoveEmptyDirs_Button_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("你確定要移除空資料夾嗎?", "確認提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Global.TimerStartTime = DateTime.Now;
                Global.TotalList = new List<int>() { 0, 0, 0, 0 };
                Common_SwitchSetUI(false);

                SongMaintenance_Tooltip_Label.Text = "正在解析空資料夾,請稍待...";

                var tasks = new List<Task>();
                tasks.Add(Task.Factory.StartNew(() => SongMaintenance_RemoveEmptyDirsTask(Global.SongMgrDestFolder, false)));

                Task.Factory.ContinueWhenAll(tasks.ToArray(), EndTask =>
                {
                    Global.TimerEndTime = DateTime.Now;
                    this.BeginInvoke((Action)delegate()
                    {
                        if (Global.TotalList[0] == 0)
                        {
                            SongMaintenance_Tooltip_Label.Text = "恭喜！在你的歌庫資料夾裡沒有發現任何空白資料夾。";
                        }
                        else
                        {
                            SongMaintenance_Tooltip_Label.Text = "總共移除 " + Global.TotalList[0] + " 個空資料夾,共花費 " + (long)(Global.TimerEndTime - Global.TimerStartTime).TotalSeconds + " 秒完成。";
                        }
                        Common_SwitchSetUI(true);
                    });
                });
            }
        }

        private void SongMaintenance_RemoveEmptyDirsTask(string dir, bool stepBack)
        {
            string RootDir = Global.SongMgrDestFolder;
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;

            if (Directory.GetFileSystemEntries(dir).Length > 0)
            {
                if (!stepBack)
                {
                    foreach (string subdir in Directory.GetDirectories(dir))
                    {
                        SongMaintenance_RemoveEmptyDirsTask(subdir, false);
                    }
                }
            }
            else
            {
                DirectoryInfo dirinfo = new DirectoryInfo(dir);
                if ((dirinfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    dirinfo.Attributes = dirinfo.Attributes & ~FileAttributes.ReadOnly;
                }

                try
                {
                    Directory.Delete(dir);
                    Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【移除空資料夾】以下為已移除的空資料夾: " + dir;
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                    lock (LockThis) { Global.TotalList[0]++; }
                }
                catch
                {
                    Global.SongLogDT.Rows.Add(Global.SongLogDT.NewRow());
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][0] = "【移除空資料夾】無法移除因資料夾已被占用: " + dir;
                    Global.SongLogDT.Rows[Global.SongLogDT.Rows.Count - 1][1] = Global.SongLogDT.Rows.Count;
                }

                string prevDir = dir.Substring(0, dir.LastIndexOf("\\"));
                if (RootDir.Length <= prevDir.Length) SongMaintenance_RemoveEmptyDirsTask(prevDir, true);

                this.BeginInvoke((Action)delegate()
                {
                    SongMaintenance_Tooltip_Label.Text = "已移除掉 " + Global.TotalList[0] + " 個空資料夾,請稍待...";
                });
            }
        }







    }




    class SongMaintenance
    {
        public static void CreateSongDataTable()
        {
            Global.SongDT = new DataTable();
            string SongQuerySqlStr = "select Song_Id, Song_Path, Song_SongName, Song_Singer, Song_Volume, Song_Track, Song_Lang, Song_FileName, Song_SingerType, Song_SongType from ktv_Song order by Song_Id";
            Global.SongDT = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongQuerySqlStr, "");

            Global.SingerDT = new DataTable();
            string SongSingerQuerySqlStr = "select Singer_Id, Singer_Name from ktv_Singer";
            Global.SingerDT = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongSingerQuerySqlStr, "");

            Global.AllSingerDT = new DataTable();
            string SongAllSingerQuerySqlStr = "select Singer_Id, Singer_Name from ktv_AllSinger";
            Global.AllSingerDT = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongAllSingerQuerySqlStr, "");

            Global.PhoneticsDT = new DataTable();
            string SongPhoneticsQuerySqlStr = "select * from ktv_Phonetics";
            Global.PhoneticsDT = CommonFunc.GetOleDbDataTable(Global.CrazyktvDatabaseFile, SongPhoneticsQuerySqlStr, "");
        }

        public static void DisposeSongDataTable()
        {
            Global.SongDT.Dispose();
            Global.SingerDT.Dispose();
            Global.AllSingerDT.Dispose();
            Global.PhoneticsDT.Dispose();
        }

        public static string GetNextSongId(string LangStr)
        {
            string NewSongID = "";
            // 查詢歌曲編號有無斷號
            if (Global.NotExistsSongIdDT.Rows.Count != 0)
            {
                string RemoveRowindex = "";
                var Query = from row in Global.NotExistsSongIdDT.AsEnumerable()
                            where row.Field<string>("Song_Lang").Equals(LangStr)
                            orderby row.Field<string>("Song_Id")
                            select row;

                foreach (DataRow row in Query)
                {
                    NewSongID = row["Song_Id"].ToString();
                    RemoveRowindex = Global.NotExistsSongIdDT.Rows.IndexOf(row).ToString();
                    break;
                }
                if (RemoveRowindex != "")
                {
                    DataRow row = Global.NotExistsSongIdDT.Rows[Convert.ToInt32(RemoveRowindex)];
                    Global.NotExistsSongIdDT.Rows.Remove(row);
                }
            }

            // 若無斷號查詢各語系下個歌曲編號
            if (NewSongID == "")
            {
                string MaxDigitCode = "";
                switch (Global.SongMgrMaxDigitCode)
                {
                    case "1":
                        MaxDigitCode = "D5";
                        break;
                    case "2":
                        MaxDigitCode = "D6";
                        break;
                }

                foreach (string str in Global.CrazyktvSongLangList)
                {
                    if (str == LangStr)
                    {
                        int LangIndex = Global.CrazyktvSongLangList.IndexOf(str);
                        Global.MaxIDList[LangIndex]++;
                        NewSongID = Global.MaxIDList[LangIndex].ToString(MaxDigitCode);
                        break;
                    }
                }
            }
            return NewSongID;
        }


    }
}