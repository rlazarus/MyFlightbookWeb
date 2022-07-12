//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Resources {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option or rebuild the Visual Studio project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Web.Application.StronglyTypedResourceProxyBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Schedule {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Schedule() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Resources.Schedule", global::System.Reflection.Assembly.Load("App_GlobalResources"));
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This is an automated notification to let you know that &lt;% Deleter %&gt; has deleted the following item from the schedule for &lt;% Resource %&gt; on %APP_NAME%:
        ///
        ///&lt;% ScheduleDetail %&gt;
        ///
        ///Thank-you.
        ///
        ///To contact us, please visit http://%APP_URL%%APP_ROOT%/Public/ContactMe.aspx.
        ///
        ///&lt;% TimeStamp %&gt;.
        /// </summary>
        internal static string AppointmentDeleted {
            get {
                return ResourceManager.GetString("AppointmentDeleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Item changed on schedule on %APP_NAME%.
        /// </summary>
        internal static string ChangeNotificationSubject {
            get {
                return ResourceManager.GetString("ChangeNotificationSubject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Are you sure you want to delete this item?  This cannot be undone!.
        /// </summary>
        internal static string confirmDelete {
            get {
                return ResourceManager.GetString("confirmDelete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Item added to the schedule on %APP_NAME%.
        /// </summary>
        internal static string CreateNotificationSubject {
            get {
                return ResourceManager.GetString("CreateNotificationSubject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Day View.
        /// </summary>
        internal static string Day {
            get {
                return ResourceManager.GetString("Day", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Item removed from schedule on %APP_NAME%.
        /// </summary>
        internal static string DeleteNotificationSubject {
            get {
                return ResourceManager.GetString("DeleteNotificationSubject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Item double-booked on %APP_NAME%.
        /// </summary>
        internal static string DoubleBookNotificationSubject {
            get {
                return ResourceManager.GetString("DoubleBookNotificationSubject", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Add to Google Calendar.
        /// </summary>
        internal static string DownloadApptGoogle {
            get {
                return ResourceManager.GetString("DownloadApptGoogle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Add to Outlook (.ics).
        /// </summary>
        internal static string DownloadApptICS {
            get {
                return ResourceManager.GetString("DownloadApptICS", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Add to Yahoo Calendar.
        /// </summary>
        internal static string DownloadApptYahoo {
            get {
                return ResourceManager.GetString("DownloadApptYahoo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Download event to your calendar.
        /// </summary>
        internal static string DownloadICal {
            get {
                return ResourceManager.GetString("DownloadICal", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This item overlaps with other items on the schedule..
        /// </summary>
        internal static string ErrDoubleBooked {
            get {
                return ResourceManager.GetString("ErrDoubleBooked", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The start date/time for this item must be prior to the end date/time..
        /// </summary>
        internal static string ErrInvalidDateTimes {
            get {
                return ResourceManager.GetString("ErrInvalidDateTimes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified event was not found.
        /// </summary>
        internal static string errItemNotFound {
            get {
                return ResourceManager.GetString("errItemNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please provide a description for this item..
        /// </summary>
        internal static string ErrNoDescription {
            get {
                return ResourceManager.GetString("ErrNoDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This item has no associated resource!.
        /// </summary>
        internal static string ErrNoResource {
            get {
                return ResourceManager.GetString("ErrNoResource", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please specify a timezone.
        /// </summary>
        internal static string errNoTimezone {
            get {
                return ResourceManager.GetString("errNoTimezone", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You are not authorized to edit this item..
        /// </summary>
        internal static string ErrUnauthorizedEdit {
            get {
                return ResourceManager.GetString("ErrUnauthorizedEdit", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This item has no duration.
        /// </summary>
        internal static string ErrZeroDuration {
            get {
                return ResourceManager.GetString("ErrZeroDuration", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cancel.
        /// </summary>
        internal static string EventCancel {
            get {
                return ResourceManager.GetString("EventCancel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Delete.
        /// </summary>
        internal static string EventDelete {
            get {
                return ResourceManager.GetString("EventDelete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to End Date/Time:.
        /// </summary>
        internal static string EventEnd {
            get {
                return ResourceManager.GetString("EventEnd", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Save.
        /// </summary>
        internal static string EventSave {
            get {
                return ResourceManager.GetString("EventSave", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Start Date/Time:.
        /// </summary>
        internal static string EventStart {
            get {
                return ResourceManager.GetString("EventStart", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Description:.
        /// </summary>
        internal static string EventTitle {
            get {
                return ResourceManager.GetString("EventTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Provide a description.
        /// </summary>
        internal static string EventTitleWatermark {
            get {
                return ResourceManager.GetString("EventTitleWatermark", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 1 day.
        /// </summary>
        internal static string intervalDay {
            get {
                return ResourceManager.GetString("intervalDay", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0:#,0.##} days.
        /// </summary>
        internal static string intervalDays {
            get {
                return ResourceManager.GetString("intervalDays", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 1 hour.
        /// </summary>
        internal static string intervalHour {
            get {
                return ResourceManager.GetString("intervalHour", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0:#,0.##} hours.
        /// </summary>
        internal static string intervalHours {
            get {
                return ResourceManager.GetString("intervalHours", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to 1 week.
        /// </summary>
        internal static string intervalWeek {
            get {
                return ResourceManager.GetString("intervalWeek", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} weeks.
        /// </summary>
        internal static string intervalWeeks {
            get {
                return ResourceManager.GetString("intervalWeeks", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to (Please choose a timezone).
        /// </summary>
        internal static string ItemEmptyTimezone {
            get {
                return ResourceManager.GetString("ItemEmptyTimezone", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Month View.
        /// </summary>
        internal static string Month {
            get {
                return ResourceManager.GetString("Month", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This is an automated notification to let you know that &lt;% Creator %&gt; has added the following item to the schedule for &lt;% Resource %&gt; on %APP_NAME%:
        ///
        ///&lt;% ScheduleDetail %&gt;
        ///
        ///Thank-you.
        ///
        ///To contact us, please visit http://%APP_URL%%APP_ROOT%/Public/ContactMe.aspx.
        ///.
        /// </summary>
        internal static string ResourceBooked {
            get {
                return ResourceManager.GetString("ResourceBooked", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This is an automated notification to let you know that &lt;% Deleter %&gt; has modified the following item from the schedule for &lt;% Resource %&gt; on %APP_NAME%:
        ///
        ///Original item:
        ///&lt;% ScheduleDetail %&gt;
        ///
        ///New Item:
        ///&lt;% ScheduleDetailNew %&gt;
        ///
        ///Thank-you.
        ///
        ///To contact us, please visit http://%APP_URL%%APP_ROOT%/Public/ContactMe.aspx.
        ///.
        /// </summary>
        internal static string ResourceChangedByOther {
            get {
                return ResourceManager.GetString("ResourceChangedByOther", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This is an automated notification to let you know that &lt;% Creator %&gt; has created or edited an item on the schedule for &lt;% Resource %&gt; on %APP_NAME% which conflicts with another item on the schedule.
        ///
        ///The following items are now simultaneously scheduled:
        ///  &lt;% ScheduleDetail %&gt;
        ///
        ///Thank-you.
        ///
        ///Please do not reply to this message..
        /// </summary>
        internal static string Resourcedoublebooked {
            get {
                return ResourceManager.GetString("Resourcedoublebooked", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Week View.
        /// </summary>
        internal static string Week {
            get {
                return ResourceManager.GetString("Week", resourceCulture);
            }
        }
    }
}
