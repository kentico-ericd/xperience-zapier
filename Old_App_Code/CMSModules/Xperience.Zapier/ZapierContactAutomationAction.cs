﻿using CMS.Activities;
using CMS.Automation;
using CMS.ContactManagement;
using CMS.Core;
using CMS.DataEngine;
using CMS.EventLog;
using CMS.Helpers;
using System.Linq;

namespace Xperience.Zapier
{
    public class ZapierContactAutomationAction : ContactAutomationAction
    {
        private IEventLogService mLogService;

        public IEventLogService LogService
        {
            get
            {
                if (mLogService == null)
                {
                    mLogService = Service.Resolve<IEventLogService>();
                }

                return mLogService;
            }
        }

        public override void Execute()
        {
            ActivityInfo activity = null;
            var url = GetResolvedParameter("WebhookURL", string.Empty);
            if(url != string.Empty && Contact != null)
            {
                // Try to get custom state data (activity)
                if(!DataHelper.IsEmpty(StateObject.StateCustomData))
                {
                    var activityDetailItemID = StateObject.StateCustomData[TriggerDataConstants.TRIGGER_DATA_ACTIVITY_ITEM_DETAILID];
                    var activityItemID = StateObject.StateCustomData[TriggerDataConstants.TRIGGER_DATA_ACTIVITY_ITEMID];
                    var activityValue = StateObject.StateCustomData[TriggerDataConstants.TRIGGER_DATA_ACTIVITY_VALUE];
                    var activitySiteId = StateObject.StateCustomData[TriggerDataConstants.TRIGGER_DATA_ACTIVITY_SITEID];
                    var q = ActivityInfo.Provider.Get()
                                        .TopN(1)
                                        .WhereEquals("ActivityItemID", activityItemID)
                                        .WhereEquals("ActivityItemDetailID", activityDetailItemID)
                                        .WhereEquals("ActivitySiteID", activitySiteId)
                                        .WhereEquals("ActivityContactID", Contact.ContactID)
                                        .WhereLessThan("ActivityCreated", StateObject.StateCreated.AddHours(1))
                                        .WhereGreaterThan("ActivityCreated", StateObject.StateCreated.AddHours(-1));
                    if(activity == null)
                    {
                        q.WhereNull("ActivityValue");
                    }
                    else
                    {
                        q.WhereEquals("ActivityValue", activityValue);
                    }
                    activity = q.FirstOrDefault();
                }

                ZapierHelper.SendPostToWebhook(url, new BaseInfo[] { Contact, activity });
            }
            else
            {
                LogService.LogEvent(EventTypeEnum.Warning, nameof(ZapierContactAutomationAction), "EXECUTE", $"Marketing automation '{Workflow.WorkflowDisplayName}' step 'Send contact to Zapier' couldn't be processed because it is missing the webhook URL or the contact wasn't found.");
            }
        }
    }
}