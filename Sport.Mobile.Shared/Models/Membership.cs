﻿using System;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sport.Mobile.Shared
{
	public class Membership : BaseModel
	{
		League _league;
		[JsonIgnore]
		public League League
		{
			get
			{
				if(_league == null)
				{
					Task.Run(async () => {
						_league = await AzureService.Instance.LeagueManager.Table.LookupAsync(LeagueId);
					}).Wait();
				}

				return _league;
			}
		}

		Athlete _athlete;
		[JsonIgnore]
		public Athlete Athlete
		{
			get
			{
				if(_athlete == null)
				{
					Task.Run(async () => {
						_athlete = await AzureService.Instance.AthleteManager.Table.LookupAsync(AthleteId);
					}).Wait();
				}

				return _athlete;
			}
		}

		DateTimeOffset? _abandonDate;
		public DateTimeOffset? AbandonDate
		{
			get
			{
				return _abandonDate;
			}
			set
			{
				SetPropertyChanged(ref _abandonDate, value);
				SetPropertyChanged("IsAbandoned");
			}
		}

		[JsonIgnore]
		public bool IsAbandoned
		{
			get
			{
				return AbandonDate.HasValue;
			}
		}

		string _athleteId;

		public string AthleteId
		{
			get
			{
				return _athleteId;
			}
			set
			{
				SetPropertyChanged(ref _athleteId, value);
				SetPropertyChanged("Athlete");
			}
		}

		string _leagueId;

		public string LeagueId
		{
			get
			{
				return _leagueId;
			}
			set
			{
				SetPropertyChanged(ref _leagueId, value);
				SetPropertyChanged("League");
			}
		}

		int _currentRank;

		public int CurrentRank
		{
			get
			{
				return _currentRank;
			}
			set
			{
				SetPropertyChanged(ref _currentRank, value);
				SetPropertyChanged("CurrentRankDisplay");
				SetPropertyChanged("CurrentRankOrdinal");
			}
		}

		[JsonIgnore]
		public int CurrentRankDisplay
		{
			get
			{
				return CurrentRank + 1;
			}
		}

		[JsonIgnore]
		public string CurrentRankOrdinal
		{
			get
			{
				return CurrentRankDisplay.ToOrdinal();
			}
		}

		DateTime? _lastRankChange;

		public DateTime? LastRankChange
		{
			get
			{
				return _lastRankChange;
			}
			set
			{
				SetPropertyChanged(ref _lastRankChange, value);
			}
		}

		[JsonIgnore]
		public Challenge OngoingChallenge
		{
			get
			{
				var challenge = League?.OngoingChallenges?.FirstOrDefault(c => c.InvolvesAthlete(AthleteId));
				return challenge;
			}
		}

		bool _isAdmin;

		public bool IsAdmin
		{
			get
			{
				return _isAdmin;
			}
			set
			{
				SetPropertyChanged(ref _isAdmin, value);
			}
		}

		[JsonIgnore]
		public DateTime LastRankChangeDate
		{
			get
			{
				return LastRankChange == null ? CreatedAt.Value : LastRankChange.Value; 
			}
		}

		[JsonIgnore]
		public string RankDescription
		{
			get
			{
				var dayCount = Math.Round(DateTime.UtcNow.Subtract(LastRankChangeDate).TotalDays);
				return string.Format("{0} out of {1} for {2} day{3}", CurrentRankDisplay.ToOrdinal(), League.Memberships?.Count, dayCount, dayCount == 1 ? "" : "s");
			}
		}

		public override void LocalRefresh()
		{
			_athlete = null;
			_league = null;

			NotifyPropertiesChanged();
		}

		public override void NotifyPropertiesChanged()
		{
			base.NotifyPropertiesChanged();
			SetPropertyChanged("LastRankChangeDate");
			SetPropertyChanged("CurrentRank");
			SetPropertyChanged("CurrentRankOrdinal");
			SetPropertyChanged("OngoingChallenge");
		}

		public Challenge GetOngoingChallenge(Athlete athlete)
		{
			//Check to see if they are part of the same league
			var membership = athlete.Memberships.SingleOrDefault(m => m.LeagueId == LeagueId);
			return membership != null ? League.OngoingChallenges?.InvolvingAthlete(athlete.Id) : null;
		}

		public bool HasExistingChallengeWithAthlete(Athlete athlete)
		{
			return GetOngoingChallenge(athlete) != null;
		}

		public override bool Equals(object obj)
		{
			var comp = new MembershipComparer();
			return comp.Equals(this, obj as Membership);
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}

	#region Comparers

	public class MembershipComparer : IEqualityComparer<Membership>
	{
		public bool Equals(Membership x, Membership y)
		{
			if(x == null || y == null)
				return false;

			var isEqual = x.Id == y.Id && x.UpdatedAt == y.UpdatedAt && x.CurrentRank == y.CurrentRank;

			if(isEqual && x.OngoingChallenge != null && y.OngoingChallenge != null)
				isEqual = x.OngoingChallenge.Id == y.OngoingChallenge.Id;

			if((x.OngoingChallenge == null && y.OngoingChallenge != null) || (x.OngoingChallenge != null && y.OngoingChallenge == null))
				return false;

			return isEqual;
		}

		public int GetHashCode(Membership obj)
		{
			return obj.Id != null ? obj.Id.GetHashCode() : base.GetHashCode();
		}
	}

	public class MembershipSortComparer : IComparer<MembershipViewModel>
	{
		public int Compare(MembershipViewModel x, MembershipViewModel y)
		{
			if(x.Membership.CurrentRank == y.Membership.CurrentRank)
				return 0;
			
			if(x.Membership.CurrentRank < y.Membership.CurrentRank)
				return -1;

			return 1;
		}
	}

	#endregion
}