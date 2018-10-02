//using eFormCore;
//using eFormShared;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsServiceHistoryPlugin;

namespace HistorySql
{
    class SqlController : LogWriter
    {
        Log log = null;
        Tools t = new Tools();

        object _writeLock = new object();

        string connectionStr;

        public SqlController(string connectionString)
        {
            connectionStr = connectionString;

            #region migrate if needed
            try
            {
                using (var db = GetContext())
                {
                    db.Database.CreateIfNotExists();
                    var match = db.Settings.Count();
                }
            }
            catch
            {
                MigrateDb();
            }
            #endregion

            //region set default for settings if needed
            if (SettingCheckAll().Count > 0)
                SettingCreateDefaults();
        }

        private MicrotingContextInterface GetContext()
        {
            return new MicrotingDbMs(connectionStr);
        }

        public bool MigrateDb()
        {
            var configuration = new Configuration();
            configuration.TargetDatabase = new DbConnectionInfo(connectionStr, "System.Data.SqlClient");
            var migrator = new DbMigrator(configuration);

            migrator.Update();
            return true;
        }

        #region public setting
        public bool SettingCreateDefaults()
        {
            //key point
            //SettingCreate(Settings.firstRunDone);
            SettingCreate(Settings.logLevel);
            SettingCreate(Settings.logLimit);
            SettingCreate(Settings.microtingDb);
            //SettingCreate(Settings.checkLast_At);
            //SettingCreate(Settings.checkPreSend_Hours);
            //SettingCreate(Settings.checkRetrace_Hours);
            //SettingCreate(Settings.checkEvery_Mins);
            //SettingCreate(Settings.includeBlankLocations);
            //SettingCreate(Settings.colorsRule);
            //SettingCreate(Settings.responseBeforeBody);
            //SettingCreate(Settings.calendarName);
            //SettingCreate(Settings.userEmailAddress);
            SettingCreate(Settings.maxParallelism);
            SettingCreate(Settings.numberOfWorkers);

            return true;
        }

        public bool SettingCreate(Settings name)
        {
            using (var db = GetContext())
            {
                //key point
                #region id = settings.name
                int id = -1;
                string defaultValue = "default";
                switch (name)
                {
                    //case Settings.firstRunDone: id = 1; defaultValue = "false"; break;
                    case Settings.logLevel: id = 2; defaultValue = "4"; break;
                    case Settings.logLimit: id = 3; defaultValue = "250"; break;
                    #region  
                    //case Settings.microtingDb: id =  4;    defaultValue = 'MicrotingDB'; break;
                    case Settings.microtingDb:

                        string microtingConnectionString = "...missing...";
                        try
                        {
                            microtingConnectionString = connectionStr.Replace("History", "SDK");
                            //SettingUpdate(Settings.firstRunDone, "true");
                        }
                        catch { }
                        id = 4; defaultValue = microtingConnectionString; break;
                    #endregion
                    //case Settings.checkLast_At: id = 5; defaultValue = DateTime.Now.AddMonths(-3).ToString(); break;
                    //case Settings.checkPreSend_Hours: id = 6; defaultValue = "36"; break;
                    //case Settings.checkRetrace_Hours: id = 7; defaultValue = "36"; break;
                    //case Settings.checkEvery_Mins: id = 8; defaultValue = "15"; break;
                    //case Settings.includeBlankLocations: id = 9; defaultValue = "true"; break;
                    //case Settings.colorsRule: id = 10; defaultValue = "1"; break;
                    //case Settings.responseBeforeBody: id = 11; defaultValue = "false"; break;
                    //case Settings.calendarName: id = 12; defaultValue = "Calendar"; break;
                    //case Settings.userEmailAddress: id = 13; defaultValue = "no-reply@invalid.invalid"; break;
                    case Settings.maxParallelism: id = 14; defaultValue = "1"; break;
                    case Settings.numberOfWorkers: id = 15; defaultValue = "1"; break;

                    default:
                        throw new IndexOutOfRangeException(name.ToString() + " is not a known/mapped Settings type");
                }
                #endregion

                settings matchId = db.Settings.SingleOrDefault(x => x.id == id);
                settings matchName = db.Settings.SingleOrDefault(x => x.name == name.ToString());

                if (matchName == null)
                {
                    if (matchId != null)
                    {
                        #region there is already a setting with that id but different name
                        //the old setting data is copied, and new is added
                        settings newSettingBasedOnOld = new settings();
                        newSettingBasedOnOld.id = (db.Settings.Select(x => (int?)x.id).Max() ?? 0) + 1;
                        newSettingBasedOnOld.name = matchId.name.ToString();
                        newSettingBasedOnOld.value = matchId.value;

                        db.Settings.Add(newSettingBasedOnOld);

                        matchId.name = name.ToString();
                        matchId.value = defaultValue;

                        db.SaveChanges();
                        #endregion
                    }
                    else
                    {
                        //its a new setting
                        settings newSetting = new settings();
                        newSetting.id = id;
                        newSetting.name = name.ToString();
                        newSetting.value = defaultValue;

                        db.Settings.Add(newSetting);
                    }
                    db.SaveChanges();
                }
                else
                    if (string.IsNullOrEmpty(matchName.value))
                    matchName.value = defaultValue;
            }

            return true;
        }

        public string SettingRead(Settings name)
        {
            try
            {
                using (var db = GetContext())
                {
                    settings match = db.Settings.Single(x => x.name == name.ToString());

                    if (match.value == null)
                        return "";

                    return match.value;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(t.GetMethodName("SQLController") + " failed", ex);
            }
        }

        public void SettingUpdate(Settings name, string newValue)
        {
            try
            {
                using (var db = GetContext())
                {
                    settings match = db.Settings.SingleOrDefault(x => x.name == name.ToString());

                    if (match == null)
                    {
                        SettingCreate(name);
                        match = db.Settings.Single(x => x.name == name.ToString());
                    }

                    match.value = newValue;
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(t.GetMethodName("SQLController") + " failed", ex);
            }
        }

        public List<string> SettingCheckAll()
        {
            List<string> result = new List<string>();
            try
            {
                using (var db = GetContext())
                {
                    int countVal = db.Settings.Count(x => x.value == "");
                    int countSet = db.Settings.Count();

                    if (countSet == 0)
                    {
                        result.Add("NO SETTINGS PRESENT, NEEDS PRIMING!");
                        return result;
                    }

                    foreach (var setting in Enum.GetValues(typeof(Settings)))
                    {
                        try
                        {
                            string readSetting = SettingRead((Settings)setting);
                            if (readSetting == "")
                                result.Add(setting.ToString() + " has an empty value!");
                        }
                        catch
                        {
                            result.Add("There is no setting for " + setting + "! You need to add one");
                        }
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(t.GetMethodName("SQLController") + " failed", ex);
            }
        }
        #endregion

        #region public check_list

        public check_lists CheckListRead(int id)
        {
            using (var db = GetContext())
            {
                return db.Check_lists.SingleOrDefault(x => x.id == id);
            }
        }

        public List<fields> FieldsOnCheckList(int check_list_id)
        {
            using (var db = GetContext())
            {
                return db.Fields.Where(x => x.check_list_id == check_list_id).ToList();
            }
        }

        #endregion


        #region public write log
        public Log StartLog(Core core)
        {
            try
            {
                string logLevel = SettingRead(Settings.logLevel);
                int logLevelInt = int.Parse(logLevel);
                if (log == null)
                    log = new Log(core, this, logLevelInt);
                return log;
            }
            catch (Exception ex)
            {
                throw new Exception(t.GetMethodName("SQLController") + " failed", ex);
            }
        }

        public override string WriteLogEntry(LogEntry logEntry)
        {
            lock (_writeLock)
            {
                try
                {
                    using (var db = GetContext())
                    {
                        logs newLog = new logs();
                        newLog.created_at = logEntry.Time;
                        newLog.level = logEntry.Level;
                        newLog.message = logEntry.Message;
                        newLog.type = logEntry.Type;

                        db.Logs.Add(newLog);
                        db.SaveChanges();

                        if (logEntry.Level < 0)
                            WriteLogExceptionEntry(logEntry);

                        #region clean up of log table
                        int limit = t.Int(SettingRead(Settings.logLimit));
                        if (limit > 0)
                        {
                            List<logs> killList = db.Logs.Where(x => x.id <= newLog.id - limit).ToList();

                            if (killList.Count > 0)
                            {
                                db.Logs.RemoveRange(killList);
                                db.SaveChanges();
                            }
                        }
                        #endregion
                    }
                    return "";
                }
                catch (Exception ex)
                {
                    return t.PrintException(t.GetMethodName("SQLController") + " failed", ex);
                }
            }
        }

        private string WriteLogExceptionEntry(LogEntry logEntry)
        {
            try
            {
                using (var db = GetContext())
                {
                    log_exceptions newLog = new log_exceptions();
                    newLog.created_at = logEntry.Time;
                    newLog.level = logEntry.Level;
                    newLog.message = logEntry.Message;
                    newLog.type = logEntry.Type;

                    db.Log_exceptions.Add(newLog);
                    db.SaveChanges();

                    #region clean up of log exception table
                    int limit = t.Int(SettingRead(Settings.logLimit));
                    if (limit > 0)
                    {
                        List<log_exceptions> killList = db.Log_exceptions.Where(x => x.id <= newLog.id - limit).ToList();

                        if (killList.Count > 0)
                        {
                            db.Log_exceptions.RemoveRange(killList);
                            db.SaveChanges();
                        }
                    }
                    #endregion
                }
                return "";
            }
            catch (Exception ex)
            {
                return t.PrintException(t.GetMethodName("SQLController") + " failed", ex);
            }
        }

        public override void WriteIfFailed(string logEntries)
        {
            lock (_writeLock)
            {
                try
                {
                    File.AppendAllText(@"expection.txt",
                        DateTime.Now.ToString() + " // " + "L:" + "-22" + " // " + "Write logic failed" + " // " + Environment.NewLine
                        + logEntries + Environment.NewLine);
                }
                catch
                {
                    //magic
                }
            }
        }
        #endregion
    }


    public enum Settings
    {
        firstRunDone,
        logLevel,
        logLimit,
        microtingDb,
        maxParallelism,
        numberOfWorkers
    }
}
