using System;

namespace DeploymentToolkit.Deployment.Settings
{
    public class DeferSettings
    {
        public int Days { get; set; }
        public string Deadline
        {
            get
            {
                return DeadlineAsDate.ToShortDateString();
            }
            set 
            {
                if(!DateTime.TryParse(value, out var date))
                    DeadlineAsDate = DateTime.MinValue.ToShortDateString();
                DeadlineAsDate = date;
            }
        }
        public DateTime DeadlineAsDate { get; private set; }
    }
}
