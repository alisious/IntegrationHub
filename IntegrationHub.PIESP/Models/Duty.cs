using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.PIESP.Models
{
    /// <summary>
    /// Status służby patrolowej.
    /// </summary>
    public enum DutyStatus { Planned, InProgress, Finished }

    /// <summary>
    /// Reprezentuje pojedynczą służbę przypisaną do użytkownika.
    /// </summary>
    public class Duty
    {
        /// <summary>Identyfikator służby.</summary>
        public int Id { get; set; }

        /// <summary>Numer odznaki użytkownika przypisanego do służby.</summary>
        public string BadgeNumber { get; set; }

        /// <summary>Rodzaj służby (np. "Patrol zapobiegawczy").</summary>
        public string Type { get; set; }

        /// <summary>Data rozpoczęcia służby (planowana).</summary>
        public DateTime PlannedStartDate { get; set; }

        /// <summary>Godzina rozpoczęcia służby (planowana).</summary>
        public TimeSpan PlannedStartTime { get; set; }

        /// <summary>Jednostka organizacyjna (np. OŻW Bydgoszcz).</summary>
        public string Unit { get; set; }

        /// <summary>Aktualny status służby.</summary>
        public DutyStatus Status { get; set; }

        /// <summary>Faktyczny czas rozpoczęcia służby.</summary>
        public DateTime? ActualStart { get; set; }

        /// <summary>Faktyczny czas zakończenia służby.</summary>
        public DateTime? ActualEnd { get; set; }
    }

}
