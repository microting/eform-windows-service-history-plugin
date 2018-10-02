/*
The MIT License (MIT)

Copyright (c) 2014 microting

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/


using System;
using System.Threading;
using Rebus.Bus;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Microting.WindowsService.BasePn;
using System.ComponentModel.Composition;
using System.IO;
using HistorySql;
using WindowsServiceHistoryPlugin.Installers;
using System.Collections.Generic;

namespace WindowsServiceHistoryPlugin
{
    [Export(typeof(ISdkEventHandler))]
    public class Core : ISdkEventHandler
    {
        #region var
        SqlController sqlController;
        Tools t = new Tools();
        private eFormCore.Core sdkCore;
        public Log log;
        IWindsorContainer container;
        public IBus bus;
        bool coreThreadRunning = false;
        bool coreRestarting = false;
        bool coreStatChanging = false;
        bool coreAvailable = false;

        string connectionString;
        string serviceLocation;
        int maxParallelism = 1;
        int numberOfWorkers = 1;
        #endregion

        public void CaseCompleted(object sender, EventArgs args)
        {
            eFormShared.Case_Dto trigger = (eFormShared.Case_Dto)sender;
            List<fields> fields = sqlController.FieldsOnCheckList(trigger.CheckListId);
            List<eFormData.FieldValue> caseFieldValues = new List<eFormData.FieldValue>();
            if (sqlController.CheckListRead(trigger.CheckListId) != null)
            {
                List<eFormShared.Field_Dto> eFormFields = sdkCore.Advanced_TemplateFieldReadAll(trigger.CheckListId);
                List<int> field_ids = new List<int>();
                foreach (eFormShared.Field_Dto f in eFormFields)
                {
                    field_ids.Add(f.Id);
                }
                foreach (fields field in fields)
                {
                    if (field_ids.Contains(field.id))
                    {
                        eFormData.FieldValue fv =  sdkCore.Advanced_FieldValueReadList(field.id, 1)[0];
                        caseFieldValues.Add(fv);
                    }
                }
            }

            sdkCore.CaseDelete(trigger.CheckListId, trigger.SiteUId);

            eFormData.MainElement eform = sdkCore.TemplateRead(trigger.CheckListId);
            SetDefaultValue(eform.ElementList, caseFieldValues);

            sdkCore.CaseCreate(eform, "", trigger.SiteUId);
        }

        public void SetDefaultValue(List<eFormData.Element> elementLst, List<eFormData.FieldValue> fieldValues)
        {
            foreach (eFormData.Element element in elementLst)
            {
                if (element.GetType() == typeof(eFormData.DataElement))
                {
                    eFormData.DataElement dataE = (eFormData.DataElement)element;
                    foreach(eFormData.DataItem item in dataE.DataItemList)
                    {
                        if (item.GetType() == typeof(eFormData.NumberStepper))
                        {
                            eFormData.NumberStepper numberStepper = (eFormData.NumberStepper)item;
                            foreach (eFormData.FieldValue fv in fieldValues)
                            {
                                if (fv.FieldId == item.Id)
                                {
                                    numberStepper.DefaultValue = int.Parse(fv.Value);
                                }
                            }

                        }
                        if (item.GetType() == typeof(eFormData.Number))
                        {
                            eFormData.Number numberStepper = (eFormData.Number)item;
                            foreach (eFormData.FieldValue fv in fieldValues)
                            {
                                if (fv.FieldId == item.Id)
                                {
                                    numberStepper.DefaultValue = int.Parse(fv.Value);
                                }
                            }

                        }
                        if (item.GetType() == typeof(eFormData.Comment))
                        {
                            eFormData.Comment comment = (eFormData.Comment)item;
                            foreach (eFormData.FieldValue fv in fieldValues)
                            {
                                if (fv.FieldId == item.Id)
                                {
                                    comment.Value = fv.Value;
                                }
                            }

                        }
                        if (item.GetType() == typeof(eFormData.Text))
                        {
                            eFormData.Text text = (eFormData.Text)item;
                            foreach (eFormData.FieldValue fv in fieldValues)
                            {
                                if (fv.FieldId == item.Id)
                                {
                                    text.Value = fv.Value;
                                }
                            }

                        }
                    }
                } else
                {
                    eFormData.GroupElement groupElement = (eFormData.GroupElement)element;
                    SetDefaultValue(groupElement.ElementList, fieldValues);
                }
            }
        }

        public void CaseDeleted(object sender, EventArgs args)
        {
            // Do nothing
        }

        public void CoreEventException(object sender, EventArgs args)
        {
            // Do nothing
        }

        public void eFormProcessed(object sender, EventArgs args)
        {
            // Do nothing
        }

        public void eFormProcessingError(object sender, EventArgs args)
        {
            // Do nothing
        }

        public void eFormRetrived(object sender, EventArgs args)
        {
            // Do nothing
        }

        public void NotificationNotFound(object sender, EventArgs args)
        {
            // Do nothing
        }

        public bool Restart(int sameExceptionCount, int sameExceptionCountMax, bool shutdownReallyFast) { return true; }

        public bool Start(string sdkConnectionString, string serviceLocation)
        {
            try
            {
                string connectionString = File.ReadAllText(serviceLocation + @"\Extensions\OutlookPlugin\sql_connection_outlook.txt").Trim();
                if (!coreAvailable && !coreStatChanging)
                {
                    this.serviceLocation = serviceLocation;
                    coreStatChanging = true;

                    if (string.IsNullOrEmpty(this.serviceLocation))
                        throw new ArgumentException("serviceLocation is not allowed to be null or empty");

                    if (string.IsNullOrEmpty(connectionString))
                        throw new ArgumentException("serverConnectionString is not allowed to be null or empty");

                    //sqlController
                    sqlController = new SqlController(connectionString);


                    //check settings
                    if (sqlController.SettingCheckAll().Count > 0)
                        throw new ArgumentException("Use AdminTool to setup database correctly. 'SettingCheckAll()' returned with errors");

                    if (sqlController.SettingRead(Settings.microtingDb) == "...missing...")
                        throw new ArgumentException("Use AdminTool to setup database correctly. microtingDb(connection string) not set, only default value found");

                    //log
                    if (log == null)
                        log = sqlController.StartLog(this);

                    try
                    {
                        maxParallelism = int.Parse(sqlController.SettingRead(Settings.maxParallelism));
                        numberOfWorkers = int.Parse(sqlController.SettingRead(Settings.numberOfWorkers));
                    }
                    catch { }

                    log.LogStandard(t.GetMethodName("Core"), "SqlController and Logger started");

                    //settings read
                    this.connectionString = connectionString;
                    log.LogStandard(t.GetMethodName("Core"), "Settings read");
                    log.LogStandard(t.GetMethodName("Core"), "this.serviceLocation is " + this.serviceLocation);

                    log.LogCritical(t.GetMethodName("Core"), "started");
                    coreAvailable = true;
                    coreStatChanging = false;

                    //coreThread
                    string sdkCoreConnectionString = sqlController.SettingRead(Settings.microtingDb);
                    startSdkCoreSqlOnly(sdkCoreConnectionString);

                    container = new WindsorContainer();
                    container.Register(Component.For<SqlController>().Instance(sqlController));
                    container.Register(Component.For<eFormCore.Core>().Instance(sdkCore));
                    container.Register(Component.For<Log>().Instance(log));
                    container.Install(
                        new RebusHandlerInstaller()
                        , new RebusInstaller(connectionString, maxParallelism, numberOfWorkers)
                    );


                    this.bus = container.Resolve<IBus>();
                    log.LogStandard(t.GetMethodName("Core"), "CoreThread started");
                }
            }
            #region catch
            catch (Exception ex)
            {
                log.LogException(t.GetMethodName("Core"), "Start failed", ex, false);
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Start failed " + ex.Message);
                throw ex;
            }
            #endregion

            return true;
            throw new NotImplementedException();
        }

        public bool Stop(bool shutdownReallyFast)
        {
            try
            {
                if (coreAvailable && !coreStatChanging)
                {
                    coreStatChanging = true;

                    coreAvailable = false;
                    log.LogCritical(t.GetMethodName("Core"), "called");

                    int tries = 0;
                    while (coreThreadRunning)
                    {
                        Thread.Sleep(100);
                        bus.Dispose();
                        tries++;
                    }

                    log.LogStandard(t.GetMethodName("Core"), "Core closed");
                    sqlController = null;
                    sdkCore.Close();

                    coreStatChanging = false;
                }
            }
            catch (ThreadAbortException)
            {
                //"Even if you handle it, it will be automatically re-thrown by the CLR at the end of the try/catch/finally."
                Thread.ResetAbort(); //This ends the re-throwning
            }
            catch (Exception ex)
            {
                log.LogException(t.GetMethodName("Core"), "Core failed to close", ex, false);
                throw ex;
            }
            return true;
        }

        public void UnitActivated(object sender, EventArgs args)
        {
            // Do nothing
        }


        public void startSdkCoreSqlOnly(string sdkConnectionString)
        {
            this.sdkCore = new eFormCore.Core();

            sdkCore.StartSqlOnly(sdkConnectionString);
        }
    }
}
