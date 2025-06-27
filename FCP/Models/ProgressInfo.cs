namespace FCP.Models
{
    public class ProgressInfo
    {
        /// <summary>
        /// The percentage of completion (0-100).
        /// </summary>
        public int Percentage { get; set; }

        /// <summary>
        /// A string describing the current file being processed.
        /// </summary>
        public string CurrentFile { get; set; }
    }
}