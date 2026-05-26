using Model;

namespace BabySitter
{
    /// <summary>
    /// ViewModel that wraps a BabySitterTeens with computed display data (rating).
    /// Not stored in the database — calculated client-side from BabySitterRate records.
    /// </summary>
    public class BabySitterCardVM
    {
        public BabySitterTeens Teen { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
    }
}
