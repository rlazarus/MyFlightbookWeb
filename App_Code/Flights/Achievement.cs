﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Configuration;
using MySql.Data.MySqlClient;
using MyFlightbook.Geography;
using MyFlightbook.Airports;
using MyFlightbook.FlightCurrency;
using System.Globalization;

/******************************************************
 * 
 * Copyright (c) 2015 MyFlightbook LLC
 * Contact myflightbook@gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook.Achievements
{
    /// <summary>
    /// Describes something that can be achieved to earn badges.  E.g., record 100 flights, get a new rating, etc.
    /// </summary>
    public class Achievement
    {
        /// <summary>
        /// For efficiency, we only compute a user's status periodically.
        /// </summary>
        public enum ComputeStatus { Never = 0, UpToDate, NeedsComputing, InProgress };

        public const string KeyVisitedAirports = "keyVisitedAirports";

        #region properties
        /// <summary>
        /// The user on whose behalf we are computing the achievements
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// A dictionary with context that can be used by badges to prevent duplicate computation
        /// </summary>
        public Dictionary<string, Object> BadgeContext { get; set; }
        #endregion

        #region Object Creation
        public Achievement()
        {
            UserName = string.Empty;
            BadgeContext = new Dictionary<string, object>();
        }

        /// <summary>
        /// Creates an achievement object
        /// </summary>
        /// <param name="szUser"></param>
        /// <param name="id"></param>
        public Achievement(string szUser) : this()
        {
            UserName = szUser;
        }
        #endregion

        #region Badge Computation
        /// <summary>
        /// Compute all of the badges for the specified user
        /// </summary>
        /// <param name="lstAdded">The list of badges that are new or changed since the previous computation</param>
        /// <param name="lstRemoved">The list of badges that have been removed</param>
        /// <returns>A list of the ACHIEVED badges (should match the database by the time this returns)</returns>
        public List<Badge> BadgesForUser()
        {
            if (String.IsNullOrEmpty(UserName))
                throw new MyFlightbookException("Cannot compute milestones on an empty user!");

            Profile pf = Profile.GetUser(UserName);

            List<Badge> lAdded = new List<Badge>();
            List<Badge> lRemoved = new List<Badge>();

            if (pf.AchievementStatus == ComputeStatus.InProgress)
                return null;

            List<Badge> lstInit = Badge.EarnedBadgesForUser(UserName);

            if (pf.AchievementStatus == ComputeStatus.UpToDate)
                return lstInit;

            List<Badge> lstTotal = Badge.AvailableBadgesForUser(UserName);

            // OK, if we're here we are either invalid or have never computed.
            // Set In Progress:
            pf.SetAchievementStatus(ComputeStatus.InProgress);

            // Pre-fill context with Visitedairports, since we know we're going to use that a bunch
            BadgeContext[KeyVisitedAirports] = VisitedAirport.VisitedAirportsForUser(UserName);

            try
            {
                // get all custom flight properties that could contribute to currency of one sort or another
                // and stick them into a dictionary for retrieval down below by flightID.
                Dictionary<int, List<CustomFlightProperty>> dctFlightEvents = new Dictionary<int, List<CustomFlightProperty>>();     // flight events (IPC, Instrument checkrides, etc.), keyed by flight ID
                IEnumerable<CustomFlightProperty> rgPfe = CustomFlightProperty.GetFlaggedEvents(UserName);
                foreach (CustomFlightProperty pfe in rgPfe)
                {
                    List<CustomFlightProperty> lstpf = (dctFlightEvents.ContainsKey(pfe.FlightID) ? dctFlightEvents[pfe.FlightID] : null);
                    if (lstpf == null)
                        dctFlightEvents.Add(pfe.FlightID, lstpf = new List<CustomFlightProperty>());
                    lstpf.Add(pfe);
                }

                // We do 3 passes against the badges:
                // 1st pass: setup/initialize
                lstTotal.ForEach((b) => { b.PreFlight(BadgeContext); });

                // 2nd pass: examine each flight IN CHRONOLOGICAL ORDER
                DBHelper dbh = new DBHelper(CurrencyExaminer.CurrencyQuery(CurrencyExaminer.CurrencyQueryDirection.Ascending));
                dbh.ReadRows(
                    (comm) =>
                    {
                        comm.Parameters.AddWithValue("UserName", UserName);
                        comm.Parameters.AddWithValue("langID", System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName);
                    },
                    (dr) =>
                    {
                        ExaminerFlightRow cfr = new ExaminerFlightRow(dr);

                        if (dctFlightEvents.ContainsKey(cfr.flightID))
                            cfr.AddEvents(dctFlightEvents[cfr.flightID]);

                        lstTotal.ForEach((b) => {
                            if (cfr.fIsRealAircraft || b.CanEarnInSim)
                                b.ExamineFlight(cfr, BadgeContext); });
                    });

                // 3rd pass: wrap up
                lstTotal.ForEach((b) => { b.PostFlight(BadgeContext); });

                lstTotal.ForEach((b) =>
                    {
                        // see if this is in the initial set.
                        Badge bMatch = lstInit.Find(bInit => bInit.ID == b.ID);

                        // Save the new badge if it is new (bmatch is null) or if it has changed (e.g., achieved new level
                        if (b.IsAchieved && !b.IsEqualTo(bMatch))
                        {
                            lAdded.Add(b);
                            b.Commit();
                        }
                        // Otherwise, delete the new badge if it exists but is no longer achieved
                        else if (!b.IsAchieved && bMatch != null && bMatch.IsAchieved)
                        {
                            lRemoved.Add(b);
                            b.Delete();
                        }
                    });
            }
            catch 
            {
                pf.SetAchievementStatus(ComputeStatus.NeedsComputing);
            }
            finally
            {
                if (pf.AchievementStatus == ComputeStatus.InProgress)
                    pf.SetAchievementStatus(ComputeStatus.UpToDate);
            }

            return lstTotal.FindAll(b => b.IsAchieved);
        }
        #endregion
    }

    /// <summary>
    /// Abstract class for a badge that is awarded for meeting an achievement
    /// </summary>
    public abstract class Badge : IComparable
    {
        /// <summary>
        /// Levels of achievement - can be binary (achieved or not achieved), or have levels
        /// </summary>
        public enum AchievementLevel
        {
            None = 0,
            Achieved,
            Bronze,
            Silver,
            Gold,
            Platinum
        }

        /// <summary>
        /// Categories of badges
        /// </summary>
        public enum BadgeCategory
        {
            BadgeCategoryUnknown = 0,
            Training = 100,
            Ratings = 200,
            Milestones = 300,
            AirportList = 10000
        }

        /// <summary>
        /// IDs for specific achievements.  DO NOT CHANGE ANY VALUES as these are persisted in the DB
        /// </summary>
        public enum BadgeID
        {
            NOOP = 0,   //  does nothing (no-op) DO NOT USE - this is an error condition!
            // First-time events
            FirstLesson = BadgeCategory.Training,
            FirstSolo,
            FirstNightLanding,
            FirstXC,
            FirstSoloXC,

            // Ratings
            PrivatePilot = BadgeCategory.Ratings,
            Instrument,
            Commercial,
            ATP,
            Sport,
            Recreational,
            CFI,
            CFII,
            MEI,

            // Multi-level badges (counts)
            NumberOfModels = BadgeCategory.Milestones,
            NumberOfAircraft,
            NumberOfFlights,
            NumberOfAirports,
            NumberOfTotalHours,
            NumberOfPICHours,
            NumberOfSICHours,
            NumberOfCFIHours,
            NumberOfNightHours,
            NumberOfIMCHours,

            AirportList00 = BadgeCategory.AirportList,
            AirportList01, AirportList02, AirportList03, AirportList04, AirportList05, AirportList06, AirportList07, AirportList08, AirportList09, AirportList10,
            AirportList11, AirportList12, AirportList13, AirportList14, AirportList15, AirportList16, AirportList17, AirportList18, AirportList19, AirportList20,
            AirportList21, AirportList22, AirportList23, AirportList24, AirportList25, AirportList26, AirportList27, AirportList28, AirportList29, AirportList30,
            AirportList31, AirportList32, AirportList33, AirportList34, AirportList35, AirportList36, AirportList37, AirportList38, AirportList39, AirportList40,
            AirportList41, AirportList42, AirportList43, AirportList44, AirportList45, AirportList46, AirportList47, AirportList48, AirportList49, AirportList50,
            AirportList51, AirportList52, AirportList53, AirportList54, AirportList55, AirportList56, AirportList57, AirportList58, AirportList59, AirportList60
        };

        #region properties
        /// <summary>
        /// The date when this was earned - could be datetime min if we don't know when it was actually earned.
        /// </summary>
        public DateTime DateEarned { get; set; }

        /// <summary>
        /// Timestamp for when this was computed
        /// </summary>
        public DateTime DateComputed { get; set; }

        /// <summary>
        /// Name of the user to whom this was awarded
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The achievement to which this corresponds
        /// </summary>
        public BadgeID ID { get; set; }

        /// <summary>
        /// The level to which it was achieved
        /// </summary>
        public AchievementLevel Level { get; set; }

        private string m_badgeName = string.Empty;
        /// <summary>
        /// The name of the badge
        /// </summary>
        public virtual string Name
        {
            get { return m_badgeName; }
            set { m_badgeName = value; }
        }

        /// <summary>
        /// Has this badge been achieved?
        /// </summary>
        public bool IsAchieved
        {
            get { return Level != AchievementLevel.None; }
        }

        public string DisplayString
        {
            get { return String.Format(CultureInfo.CurrentCulture, "{0} {1}", Name, EarnedDateString); }
        }

        public string EarnedDateString
        {
            get { return !DateEarned.HasValue() ? string.Empty : String.Format(CultureInfo.CurrentCulture, Resources.Achievements.EarnedDate, DateEarned.ToShortDateString()); }
        }

        /// <summary>
        /// If appropriate, contains the ID of the flight on which this was earned.
        /// </summary>
        public int IDFlightEarned { get; set; }

        /// <summary>
        /// Says whether or not this badge can be earned in a sim.  False for most.
        /// </summary>
        public virtual bool CanEarnInSim
        {
            get { return false;  }
        }

        /// <summary>
        /// URL to the badge image
        /// </summary>
        public virtual string BadgeImage
        {
            get
            {
                switch (Level)
                {
                    case AchievementLevel.Achieved:
                        return "~/Images/Badge-Achieved.png";
                    case AchievementLevel.Bronze:
                        return "~/Images/Badge-Bronze.png";
                    case AchievementLevel.Gold:
                        return "~/Images/Badge-Gold.png";
                    case AchievementLevel.Platinum:
                        return "~/Images/Badge-Platinum.png";
                    case AchievementLevel.Silver:
                        return "~/Images/Badge-Silver.png";
                    default:
                    case AchievementLevel.None:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Alt text for the badge image
        /// </summary>
        public string BadgeImageAltText
        {
            get
            {
                switch (Level)
                {
                    case AchievementLevel.Achieved:
                        return Resources.Achievements.badgeTitleAchieved;
                    case AchievementLevel.Bronze:
                        return Resources.Achievements.badgeTitleBronze;
                    case AchievementLevel.Gold:
                        return Resources.Achievements.badgeTitleGold;
                    case AchievementLevel.Platinum:
                        return Resources.Achievements.badgeTitlePlatinum;
                    case AchievementLevel.Silver:
                        return Resources.Achievements.badgeTitleSilver;
                    default:
                    case AchievementLevel.None:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Optional Image overlay for the badge
        /// </summary>
        public virtual string BadgeImageOverlay { get { return string.Empty; } }

        /// <summary>
        /// Category of achievement
        /// </summary>
        public BadgeCategory Category
        {
            get
            {
                BadgeCategory bc = BadgeCategory.BadgeCategoryUnknown;
                BadgeCategory[] rgCategoryBoundaries = (BadgeCategory[])Enum.GetValues(typeof(BadgeCategory));

                for (int i = 0; i < rgCategoryBoundaries.Length; i++)
                {
                    if ((int) ID >= (int) rgCategoryBoundaries[i])
                        bc = (BadgeCategory) rgCategoryBoundaries[i];
                }
                return bc;
            }
        }

        /// <summary>
        /// Returns the name of the category (localized)
        /// </summary>
        /// <param name="bc"></param>
        /// <returns></returns>
        public static string GetCategoryName(BadgeCategory bc)
        {
            switch (bc)
            {
                case BadgeCategory.Milestones:
                    return Resources.Achievements.categoryMilestones;
                case BadgeCategory.Ratings:
                    return Resources.Achievements.categoryRatings;
                case BadgeCategory.Training:
                    return Resources.Achievements.categoryTraining;
                case BadgeCategory.AirportList:
                    return Resources.Achievements.categoryVisitedAirports;
                case BadgeCategory.BadgeCategoryUnknown:
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Display name for the category
        /// </summary>
        public string CategoryName
        {
            get { return GetCategoryName(Category);}
        }

        /// <summary>
        /// Gets last error (not persisted, obviously)
        /// </summary>
        public string ErrorString {get; set;}
        #endregion

        #region Object Creation
        private void Init()
        {
            DateEarned = DateComputed = DateTime.MinValue;
            UserName = string.Empty;
            ID = BadgeID.NOOP;
            Level = AchievementLevel.None;
            IDFlightEarned = LogbookEntry.idFlightNone;
        }

        protected Badge()
        {
            Init();
        }

        protected Badge(BadgeID id, string szName)
        {
            Init();
            ID = id;
            m_badgeName = szName;
        }

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "{0} ({2}) - {1}", UserName, Name, Level.ToString());
        }
        #endregion

        #region Computation
        /// <summary>
        /// Subclassed with logic for actual achievements
        /// </summary>
        /// <param name="cfr">The CurrencyFlightRow of the flight to examine</param>
        /// <param name="context">A dictionary that can be used to share/retrieve context, preventing duplicate computation</param>
        public abstract void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, Object> context);

        /// <summary>
        /// Called before walking through the flights (perhaps to initialize visited airports, or aircraft for user, or such
        /// </summary>
        /// <param name="context">A dictionary that can be used to share/retrieve context, preventing duplicate computation</param>
        public virtual void PreFlight(Dictionary<string, Object> context) { }

        /// <summary>
        /// Called after walking through the flights (perhaps to do some final computations)
        /// </summary>
        /// <param name="context">A dictionary that can be used to share/retrieve context, preventing duplicate computation</param>
        public virtual void PostFlight(Dictionary<string, Object> context) { }
        #endregion

        #region Database
        protected virtual void InitFromDataReader(MySqlDataReader dr)
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            ID = (BadgeID)Convert.ToInt32(dr["BadgeID"], CultureInfo.InvariantCulture);
            UserName = (string)dr["Username"];
            Level = (AchievementLevel)Convert.ToInt32(dr["AchievementLevel"], CultureInfo.InvariantCulture);
            DateEarned = (DateTime)util.ReadNullableField(dr, "AchievedDate", DateTime.MinValue);
            DateComputed = Convert.ToDateTime(dr["ComputeDate"], CultureInfo.InvariantCulture);
        }

        public bool FIsValid()
        {
            ErrorString = string.Empty;

            try
            {
                if (String.IsNullOrEmpty(UserName))
                    throw new MyFlightbookValidationException("No user specified");
                if (ID == BadgeID.NOOP)
                    throw new MyFlightbookValidationException("Attempt to save invalid badge!");
                if (Level == AchievementLevel.None)
                    throw new MyFlightbookValidationException("Attempt to save un-earned badge!");
            }
            catch (MyFlightbookValidationException ex)
            {
                ErrorString = ex.Message;
            }

            return ErrorString.Length == 0;
        }

        public void Commit()
        {
            if (!FIsValid())
                throw new MyFlightbookValidationException(String.Format(CultureInfo.CurrentCulture, "Error saving badge: {0}", ErrorString));

            DBHelper dbh = new DBHelper("REPLACE INTO badges SET BadgeID=?achieveID, UserName=?username, ClassName=?classname, AchievementLevel=?level, AchievedDate=?dateearned, ComputeDate=Now()");
            dbh.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("achieveID", ID);
                    comm.Parameters.AddWithValue("username", UserName);
                    comm.Parameters.AddWithValue("classname", GetType().ToString());
                    comm.Parameters.AddWithValue("level", (int)Level);
                    if (DateEarned.CompareTo(DateTime.MinValue) == 0)
                        comm.Parameters.AddWithValue("dateearned", null);
                    else
                        comm.Parameters.AddWithValue("dateearned", DateEarned);
                });
        }

        public void Delete()
        {
            DBHelper dbh = new DBHelper("DELETE FROM badges WHERE BadgeID=?achieveID AND Username=?username");
            dbh.DoNonQuery((comm) =>
                {
                    comm.Parameters.AddWithValue("achieveID", ID);
                    comm.Parameters.AddWithValue("username", UserName);
                });
        }

        /// <summary>
        /// Get a set of badges earned by the user
        /// </summary>
        /// <param name="szUser">The username</param>
        /// <returns>A list containing the relevant badges</returns>
        public static List<Badge> EarnedBadgesForUser(string szUser)
        {
            List<Badge> lst = new List<Badge>();
            DBHelper dbh = new DBHelper("SELECT * FROM badges WHERE username=?user");
            dbh.ReadRows(
                (comm) => { comm.Parameters.AddWithValue("user", szUser); },
                (dr) =>
                {
                    Badge b = (Badge)Activator.CreateInstance(Type.GetType((string)dr["ClassName"]));
                    b.InitFromDataReader(dr);
                    lst.Add(b);
                });
            return lst;
        }

        /// <summary>
        /// Deletes all badges for the specified user
        /// </summary>
        /// <param name="szUser">Username</param>
        /// <param name="fUpdateProfile">Update the user's profile?  E.g., if deleting user's flights, then yes, we want to update the profile; if deleting the whole account, then it's pointless (or could fail if account is already deleted)</param>
        public static void DeleteBadgesForUser(string szUser, bool fUpdateProfile)
        {
            DBHelper dbh = new DBHelper("DELETE FROM badges WHERE username=?user");
            dbh.DoNonQuery((comm) => { comm.Parameters.AddWithValue("user", szUser); });

            if (fUpdateProfile)
            {
                Profile pf = Profile.GetUser(szUser);
                if (!String.IsNullOrEmpty(pf.UserName))
                    pf.SetAchievementStatus(Achievement.ComputeStatus.Never);
            }
        }
        #endregion

        /// <summary>
        /// Get a list of achievements available for the specified user
        /// </summary>
        /// <param name="szUser">The user name</param>
        /// <returns>An array of all possible Badges</returns>
        public static List<Badge> AvailableBadgesForUser(string szUser)
        {
            Badge[] rgAchievements = 
                {
                    // First-time events
                    new TrainingBadgeBegan(),
                    new TrainingBadgeFirstSolo(),
                    new TrainingBadgeFirstNightLanding(),
                    new TrainingBadgeFirstXC(),
                    new TrainingBadgeFirstSoloXC(),

                    // Ratings
                    new RatingBadgeATP(),
                    new RatingBadgeCFI(),
                    new RatingBadgeCFII(),
                    new RatingBadgeCommercial(),
                    new RatingBadgeInstrument(),
                    new RatingBadgePPL(),
                    new RatingBadgeRecreational(),
                    new RatingBadgeSport(),
                    new RatingBadgeMEI(),

                    // Multi-level badges (counts)
                    new MultiLevelBadgeNumberFlights(),
                    new MultiLevelBadgeNumberModels(),
                    new MultiLevelBadgeNumberAircraft(),
                    new MultiLevelBadgeNumberAirports(),
                    new MultiLevelBadgeTotalTime(),
                    new MultiLevelBadgePICTime(),
                    new MultiLevelBadgeSICTime(),
                    new MultiLevelBadgeCFITime(),
                    new MultiLevelBadgeIMCTime(),
                    new MultiLevelBadgeNightTime()
                };

            List<Badge> lst = new List<Badge>(rgAchievements);
            lst.AddRange(AirportListBadge.GetAirportListBadges());
            lst.ForEach((b) => { b.UserName = szUser; });
            return lst;
        }

        public int CompareTo(object obj)
        {
            Badge bCompare = (Badge)obj;
            int datecomp = DateEarned.CompareTo(bCompare.DateEarned);
            return (datecomp == 0) ? String.Compare(Name, bCompare.Name, StringComparison.CurrentCultureIgnoreCase) : datecomp;
        }

        public virtual bool IsEqualTo(Badge bCompare)
        {
            return (bCompare != null &&
                    ID == bCompare.ID &&
                    IsAchieved == bCompare.IsAchieved &&
                    Level == bCompare.Level &&
                    DateEarned.CompareTo(bCompare.DateEarned) == 0);
        }
    }

#region Concrete Badge Classes
    #region Training Badges
    /// <summary>
    /// First flight is a training flight
    /// </summary>
    public class TrainingBadgeBegan : Badge
    {
        bool fSeen1stFlight = false;

        public TrainingBadgeBegan()
            : base(BadgeID.FirstLesson, Resources.Achievements.nameFirstLesson)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (!fSeen1stFlight)
            {
                fSeen1stFlight = true;
                if (cfr.Dual > 0 && cfr.PIC == 0 && cfr.Total > 0)
                {
                    Level = AchievementLevel.Achieved;
                    DateEarned = cfr.dtFlight;
                    IDFlightEarned = cfr.flightID;
                }
            }
        }
    }

    /// <summary>
    /// Badge for first solo
    /// </summary>
    public class TrainingBadgeFirstSolo : Badge
    {
        public TrainingBadgeFirstSolo() : base(BadgeID.FirstSolo, Resources.Achievements.nameFirstSolo)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, Object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (Level == AchievementLevel.None)
            {
                if (cfr.FindEvent(pf => (pf.PropertyType.IsSolo && pf.DecValue > 0)) != null)
                {
                    Level = AchievementLevel.Achieved;
                    DateEarned = cfr.dtFlight;
                    IDFlightEarned = cfr.flightID;
                }
            }
        }
    }

    /// <summary>
    /// Badge for first night landing
    /// </summary>
    public class TrainingBadgeFirstNightLanding : Badge
    {
        bool fSeenFirstLanding = false;

        public TrainingBadgeFirstNightLanding()
            : base(BadgeID.FirstNightLanding, Resources.Achievements.nameFirstNightLanding)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (!fSeenFirstLanding && cfr.Dual > 0 && cfr.PIC == 0 && cfr.cFullStopNightLandings > 0)
            {
                fSeenFirstLanding = true;
                Level = AchievementLevel.Achieved;
                DateEarned = cfr.dtFlight;
                IDFlightEarned = cfr.flightID;
            }
        }
    }

    /// <summary>
    /// Badge for first Cross-country flight
    /// </summary>
    public class TrainingBadgeFirstXC : Badge
    {
        bool fSeen1stXC = false;

        public TrainingBadgeFirstXC()
            : base(BadgeID.FirstXC, Resources.Achievements.nameFirstXC)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (!fSeen1stXC && cfr.Dual > 0 && cfr.PIC == 0 && cfr.XC > 0)
            {
                fSeen1stXC = true;
                Level = AchievementLevel.Achieved;
                DateEarned = cfr.dtFlight;
                IDFlightEarned = cfr.flightID;
            }
        }
    }

    /// <summary>
    /// Badge for first solo cross-country flight
    /// </summary>
    public class TrainingBadgeFirstSoloXC : Badge
    {
        bool fSeen1stXC = false;

        public TrainingBadgeFirstSoloXC()
            : base(BadgeID.FirstSoloXC, Resources.Achievements.nameFirstSoloXCFlight)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (!fSeen1stXC && cfr.XC > 0 && cfr.FindEvent(pf => (pf.PropertyType.IsSolo && pf.DecValue > 0)) != null)
            {
                fSeen1stXC = true;
                Level = AchievementLevel.Achieved;
                DateEarned = cfr.dtFlight;
                IDFlightEarned = cfr.flightID;
            }
        }
    }
    #endregion

    #region Multi-level badges based on integer counts
    public abstract class MultiLevelCountBadgeBase : Badge
    {
        public int[] Levels {get; set;}
        public int ItemCount { get; set; }
        public string ProgressTemplate { get; set; }

        public override string Name
        {
            get { return String.Format(CultureInfo.CurrentCulture, ProgressTemplate, Levels[(int)Level - (int)AchievementLevel.Bronze], CultureInfo.CurrentCulture); }
            set { base.Name = value; }
        }

        protected MultiLevelCountBadgeBase(BadgeID id, string nameTemplate, int Bronze, int Silver, int Gold, int Platinum)
            : base(id, nameTemplate)
        {
            ProgressTemplate = nameTemplate;
            Levels = new int[] { Bronze, Silver, Gold, Platinum};
        }

        public override void PostFlight(Dictionary<string, Object> context)
        {
            Level = AchievementLevel.None;

            for (int i = 0; i < Levels.Length; i++)
            {
                if (ItemCount > Levels[i])
                    Level = (AchievementLevel)((int)AchievementLevel.Bronze + i);
                else
                    break;
            }
        }
    }

    #region Concrete Multi-level badges
    /// <summary>
    /// Multi-level badge for number of flights.
    /// </summary>
    public class MultiLevelBadgeNumberFlights : MultiLevelCountBadgeBase
    {
        public MultiLevelBadgeNumberFlights()
            : base(BadgeID.NumberOfFlights, Resources.Achievements.nameNumberFlights, 25, 100, 1000, 5000)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, Object> context)
        {
            ++ItemCount;
        }
    }

    /// <summary>
    /// Multi-level badge for flying a given number of models of aircraft
    /// </summary>
    public class MultiLevelBadgeNumberModels : MultiLevelCountBadgeBase
    {
        List<int> lstModelsFlown = new List<int>();

        public MultiLevelBadgeNumberModels()
            : base(BadgeID.NumberOfModels, Resources.Achievements.nameNumberModels, 25, 50, 100, 200)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, Object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (!lstModelsFlown.Contains(cfr.idModel))
                lstModelsFlown.Add(cfr.idModel);
        }

        public override void PostFlight(Dictionary<string, Object> context)
        {
            ItemCount = lstModelsFlown.Count;
            base.PostFlight(context);
        }
    }

    /// <summary>
    /// Multi-level badge for flying a given number of distinct aircraft
    /// </summary>
    public class MultiLevelBadgeNumberAircraft : MultiLevelCountBadgeBase
    {
        List<int> lstAircraftFlown = new List<int>();

        public MultiLevelBadgeNumberAircraft()
            : base(BadgeID.NumberOfAircraft, Resources.Achievements.nameNumberAircraft, 20, 50, 100, 200)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, Object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (!lstAircraftFlown.Contains(cfr.idAircraft))
                lstAircraftFlown.Add(cfr.idAircraft);
        }

        public override void PostFlight(Dictionary<string, Object> context)
        {
            ItemCount = lstAircraftFlown.Count;
            base.PostFlight(context);
        }
    }

    /// Multi-level badge for number of airports visited
    public class MultiLevelBadgeNumberAirports : MultiLevelCountBadgeBase
    {
        public MultiLevelBadgeNumberAirports()
            : base(BadgeID.NumberOfAirports, Resources.Achievements.nameNumberAirports, 50, 200, 400, 1000)
        {
        }

        public override void PostFlight(Dictionary<string, object> context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (context.ContainsKey(Achievement.KeyVisitedAirports))
            {
                VisitedAirport[] rgva = (VisitedAirport[])context[Achievement.KeyVisitedAirports];

                if (rgva != null)
                    ItemCount = rgva.Length;
            }
            base.PostFlight(context);
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context) { }
    }

    /// Multi-level badge for total time
    public class MultiLevelBadgeTotalTime : MultiLevelCountBadgeBase
    {
        decimal cHoursTotal = 0.0M;
        public MultiLevelBadgeTotalTime()
            : base(BadgeID.NumberOfTotalHours, Resources.Achievements.nameNumberTotal, 40, 500, 1000, 5000)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context) 
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            cHoursTotal += cfr.Total;
        }

        public override void PostFlight(Dictionary<string, object> context)
        {
            ItemCount = (int)cHoursTotal;
            base.PostFlight(context);
        }
    }

    /// Multi-level badge for PIC time
    public class MultiLevelBadgePICTime : MultiLevelCountBadgeBase
    {
        decimal cHoursPIC = 0.0M;
        public MultiLevelBadgePICTime()
            : base(BadgeID.NumberOfPICHours, Resources.Achievements.nameNumberPIC, 100, 500, 1000, 5000)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            cHoursPIC += cfr.PIC;
        }

        public override void PostFlight(Dictionary<string, object> context)
        {
            ItemCount = (int)cHoursPIC;
            base.PostFlight(context);
        }
    }

    /// Multi-level badge for total time
    public class MultiLevelBadgeSICTime : MultiLevelCountBadgeBase
    {
        decimal cHoursTotal = 0.0M;
        public MultiLevelBadgeSICTime()
            : base(BadgeID.NumberOfSICHours, Resources.Achievements.nameNumberSIC, 40, 500, 1000, 5000)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            cHoursTotal += cfr.SIC;
        }

        public override void PostFlight(Dictionary<string, object> context)
        {
            ItemCount = (int)cHoursTotal;
            base.PostFlight(context);
        }
    }

    /// Multi-level badge for total time
    public class MultiLevelBadgeCFITime : MultiLevelCountBadgeBase
    {
        decimal cHoursTotal = 0.0M;
        public MultiLevelBadgeCFITime()
            : base(BadgeID.NumberOfCFIHours, Resources.Achievements.nameNumberCFI, 100, 500, 1000, 5000)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            cHoursTotal += cfr.CFI;
        }

        public override void PostFlight(Dictionary<string, object> context)
        {
            ItemCount = (int)cHoursTotal;
            base.PostFlight(context);
        }
    }

    /// Multi-level badge for IMC time
    public class MultiLevelBadgeIMCTime : MultiLevelCountBadgeBase
    {
        decimal cIMCTimeTotal = 0.0M;

        public override string BadgeImageOverlay
        {
            get { return "~/images/BadgeOverlays/cloud.png"; }
        }

        public MultiLevelBadgeIMCTime()
            : base(BadgeID.NumberOfIMCHours, Resources.Achievements.nameNumberIMC, 50, 200, 500, 1000)
        {
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            cIMCTimeTotal += cfr.IMC;
        }

        public override void PostFlight(Dictionary<string, object> context)
        {
            ItemCount = (int)cIMCTimeTotal;
            base.PostFlight(context);
        }
    }

    /// Multi-level badge for Night time
    public class MultiLevelBadgeNightTime : MultiLevelCountBadgeBase
    {
        decimal cNightTotal = 0.0M;

        public override string BadgeImageOverlay
        {
            get { return "~/images/BadgeOverlays/nightowl.png"; }
        }

        public MultiLevelBadgeNightTime()
            : base(BadgeID.NumberOfNightHours, Resources.Achievements.nameNumberNight, 50, 200, 500, 1000) { }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            cNightTotal += cfr.IMC;
        }

        public override void PostFlight(Dictionary<string, object> context)
        {
            ItemCount = (int)cNightTotal;
            base.PostFlight(context);
        }
    }
    #endregion
    #endregion

    #region New Ratings
    /// <summary>
    /// Abstract base class for achieving various ratings
    /// </summary>
    public abstract class RatingBadgeBase : Badge
    {
        CustomPropertyType.KnownProperties idPropNewRating = CustomPropertyType.KnownProperties.None;

        public override string BadgeImageOverlay
        {
            get { return "~/images/BadgeOverlays/certificate.png";}
        }

        protected RatingBadgeBase(BadgeID id, string szName, CustomPropertyType.KnownProperties idprop)
            : base(id, szName)
        {
            idPropNewRating = idprop;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, Object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");

            if (Level == AchievementLevel.None)
            {
                if (cfr.GetEventWithTypeID(idPropNewRating) != null)
                {
                    Level = AchievementLevel.Achieved;
                    DateEarned = cfr.dtFlight;
                    IDFlightEarned = cfr.flightID;
                }
            }
        }
    }

    #region Concrete classes for getting new ratings
    public class RatingBadgePPL : RatingBadgeBase
    {
        public RatingBadgePPL()
            : base(BadgeID.PrivatePilot, Resources.Achievements.nameRatingPPL, CustomPropertyType.KnownProperties.IDPropCheckridePPL)
        {
        }
    }

    public class RatingBadgeInstrument : RatingBadgeBase
    {
        public RatingBadgeInstrument()
            : base(BadgeID.Instrument, Resources.Achievements.nameRatingInstrument, CustomPropertyType.KnownProperties.IDPropCheckrideIFR)
        {
        }
    }

    public class RatingBadgeCommercial : RatingBadgeBase
    {
        public RatingBadgeCommercial()
            : base(BadgeID.Commercial, Resources.Achievements.nameRatingCommercial, CustomPropertyType.KnownProperties.IDPropCheckrideCommercial)
        {
        }
    }

    public class RatingBadgeATP : RatingBadgeBase
    {
        public RatingBadgeATP()
            : base(BadgeID.ATP, Resources.Achievements.nameRatingATP, CustomPropertyType.KnownProperties.IDPropCheckrideATP)
        {
        }

        /// <summary>
        /// You can earn an ATP in a sim
        /// </summary>
        public override bool CanEarnInSim { get { return true; } }

        /// <summary>
        /// Can earn ATP in a sim if it's certified landing OR if it's a real aircraft.
        /// </summary>
        /// <param name="cfr"></param>
        /// <param name="context"></param>
        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context)
        {
            if (cfr == null)
                throw new ArgumentNullException("cfr");
            if (cfr.fIsRealAircraft || cfr.fIsCertifiedLanding)
                base.ExamineFlight(cfr, context);
        }
    }

    public class RatingBadgeSport : RatingBadgeBase
    {
        public RatingBadgeSport()
            : base(BadgeID.Sport, Resources.Achievements.nameRatingSport, CustomPropertyType.KnownProperties.IDPropCheckrideSport) { }
    }

    public class RatingBadgeRecreational : RatingBadgeBase
    {
        public RatingBadgeRecreational()
            : base(BadgeID.Recreational, Resources.Achievements.nameRatingRecreation, CustomPropertyType.KnownProperties.IDPropCheckrideRecreational) { }
    }

    public class RatingBadgeCFI : RatingBadgeBase
    {
        public RatingBadgeCFI()
            : base(BadgeID.CFI, Resources.Achievements.nameRatingCFI, CustomPropertyType.KnownProperties.IDPropCheckrideCFI) { }
    }

    public class RatingBadgeCFII : RatingBadgeBase
    {
        public RatingBadgeCFII()
            : base(BadgeID.CFII, Resources.Achievements.nameRatingCFII, CustomPropertyType.KnownProperties.IDPropCheckrideCFII) { }
    }

    public class RatingBadgeMEI : RatingBadgeBase
    {
        public RatingBadgeMEI() : base(BadgeID.MEI, Resources.Achievements.nameRatingMEI, CustomPropertyType.KnownProperties.IDPropCheckrideMEI) { }
    }
    #endregion
    #endregion

    #region AirportList Badges
    [Serializable]
    public class AirportListBadgeData
    {
        public Badge.BadgeID ID { get; set; }
        public string Name { get; set; }
        public string AirportsRaw { get; set; }
        public string OverlayName { get; set; }
        public AirportList Airports { get; set; }
        public LatLongBox BoundingFrame { get; set; }
        public int[] Levels { get; set; }
        public bool BinaryOnly { get; set; }

        public AirportListBadgeData()
        {
        }

        public AirportListBadgeData(MySqlDataReader dr) : this()
        {
            if (dr == null)
                throw new ArgumentNullException("dr");
            Name = dr["name"].ToString();
            ID = (Badge.BadgeID)Convert.ToInt32(dr["idachievement"], CultureInfo.InvariantCulture);
            AirportsRaw = dr["airportcodes"].ToString();
            Airports = new AirportList(AirportsRaw);
            BoundingFrame = Airports.LatLongBox(true).Inflate(0.1); // allow for a little slop
            OverlayName = util.ReadNullableString(dr, "overlayname");
            BinaryOnly = Convert.ToInt32(dr["fBinaryOnly"], CultureInfo.InvariantCulture) != 0;
            Levels = new int[4];
            Levels[0] = Convert.ToInt32(dr["thresholdBronze"], CultureInfo.InvariantCulture);
            Levels[1] = Convert.ToInt32(dr["thresholdSilver"], CultureInfo.InvariantCulture);
            Levels[2] = Convert.ToInt32(dr["thresholdGold"], CultureInfo.InvariantCulture);
            Levels[3] = Convert.ToInt32(dr["thresholdPlatinum"], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Adds a new airport-list badge
        /// </summary>
        /// <param name="name">Name of the badge</param>
        /// <param name="codes">Airport codes</param>
        /// <param name="overlay">Name of the overlay PNG</param>
        /// <param name="fBinary">True if this is a binary (earn/don't earn) property</param>
        /// <param name="bronze">Threshold for bronze</param>
        /// <param name="silver">Threshold for silver</param>
        /// <param name="gold">Threshold for gold</param>
        /// <param name="platinum">Threshold for platinum</param>
        public static void Add(string name, string codes, string overlay, bool fBinary, int bronze, int silver, int gold, int platinum)
        {
            DBHelper dbh = new DBHelper("INSERT INTO airportlistachievement SET name=?name, airportcodes=?airportcodes, overlayname=?overlay, fBinaryOnly=?fbinary, thresholdBronze=?bronze, thresholdSilver=?silver, thresholdGold=?gold, thresholdPlatinum=?platinum");
            dbh.DoNonQuery((comm) =>
            {
                comm.Parameters.AddWithValue("name", name);
                comm.Parameters.AddWithValue("airportcodes", codes);
                comm.Parameters.AddWithValue("overlay", overlay);
                comm.Parameters.AddWithValue("fbinary", fBinary ? 1 : 0);
                comm.Parameters.AddWithValue("bronze", bronze);
                comm.Parameters.AddWithValue("silver", silver);
                comm.Parameters.AddWithValue("gold", gold);
                comm.Parameters.AddWithValue("platinum", platinum);
            });
        }
    }

    public class AirportListBadge : MultiLevelCountBadgeBase
    {
        protected AirportListBadgeData m_badgeData { get; set; }

        public AirportListBadge() : base(BadgeID.NOOP, string.Empty, 0, 0, 0, 0) { }

        public override string BadgeImageOverlay
        {
            get { return (m_badgeData == null || String.IsNullOrEmpty(m_badgeData.OverlayName)) ? string.Empty : "~/images/BadgeOverlays/" + m_badgeData.OverlayName; }
        }

        protected AirportListBadge(AirportListBadgeData albd)
            : base(albd == null ? BadgeID.NOOP : albd.ID, albd == null ? string.Empty : albd.Name, albd == null ? 0 : albd.Levels[0], albd == null ? 0 : albd.Levels[1], albd == null ? 0 : albd.Levels[2], albd == null ? 0 : albd.Levels[3])
        {
            m_badgeData = albd;
        }

        protected override void InitFromDataReader(MySqlDataReader dr)
        {
            base.InitFromDataReader(dr);

            // get the name that matches this
            try
            {
                m_badgeData = BadgeData.Find(albd => albd.ID == ID);
                ProgressTemplate = Name = m_badgeData.Name;
                Levels = m_badgeData.Levels;
            }
            catch { }
        }

        private const string szCacheDataKey = "keyAirportListBadgesDataList";

        public static void FlushCache()
        {
            if (HttpContext.Current != null && HttpContext.Current.Cache != null)
                HttpContext.Current.Cache.Remove(szCacheDataKey);
        }

        protected static List<AirportListBadgeData> BadgeData
        {
            get
            {
                List<AirportListBadgeData> lst = null;

                if (HttpContext.Current != null && HttpContext.Current.Cache != null)
                    lst = (List<AirportListBadgeData>)HttpContext.Current.Cache[szCacheDataKey];

                if (lst == null)
                {
                    lst = new List<AirportListBadgeData>();
                    DBHelper dbh = new DBHelper("SELECT * FROM airportlistachievement");
                    dbh.ReadRows((comm) => { }, (dr) => { lst.Add(new AirportListBadgeData(dr)); });

                    if (HttpContext.Current != null && HttpContext.Current.Cache != null)
                        HttpContext.Current.Cache.Add(szCacheDataKey, lst, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(0, 30, 0), System.Web.Caching.CacheItemPriority.Default, null);
                }

                return lst;
            }
        }

        /// <summary>
        /// Get a list of all database-defined airportlist badges.
        /// </summary>
        /// <returns>The new badges</returns>
        public static List<AirportListBadge> GetAirportListBadges()
        {
            List<AirportListBadge> l = new List<AirportListBadge>();
            BadgeData.ForEach((albd) => {l.Add(new AirportListBadge(albd));});
            return l;
        }

        public override void ExamineFlight(ExaminerFlightRow cfr, Dictionary<string, object> context) { }

        public override void PostFlight(Dictionary<string, object> context)
        {
            VisitedAirport[] rgva = (VisitedAirport[])context[Achievement.KeyVisitedAirports];
            DateTime dtEarned = DateTime.MinValue;
            if (rgva != null)
            {
                List<airport> lstAirports = new List<airport>(m_badgeData.Airports.GetAirportList());
                int cAirportsHit = 0;
                lstAirports.RemoveAll(ap => !ap.IsAirport && !ap.IsSeaport);
                Array.ForEach<VisitedAirport>(rgva, (va) =>
                {
                    if (m_badgeData.BoundingFrame.ContainsPoint(va.Airport.LatLong))
                    {
                        List<airport> apMatches = lstAirports.FindAll(ap => ap.LatLong.IsSameLocation(va.Airport.LatLong, 0.02) && String.Compare(ap.FacilityTypeCode, va.Airport.FacilityTypeCode) == 0);
                        apMatches.ForEach((ap) => { lstAirports.Remove(ap); });
                        if (apMatches.Count > 0)
                        {
                            dtEarned = dtEarned.LaterDate(va.EarliestVisitDate);
                            cAirportsHit++;
                        }
                    }
                });
                if (m_badgeData.BinaryOnly)
                {
                    Level = lstAirports.Count == 0 ? AchievementLevel.Achieved : AchievementLevel.None;
                    DateEarned = dtEarned;
                }
                else
                {
                    ItemCount = cAirportsHit;
                    base.PostFlight(context);
                }   
            }
        }

        public override string Name
        {
            get { return (m_badgeData.BinaryOnly) ? m_badgeData.Name : base.Name; }
            set { base.Name = value; }
        }
    }
    #endregion
#endregion
}