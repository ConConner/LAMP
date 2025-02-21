using System.Drawing;

namespace LAMP
{
    /// <summary>
    /// A class which should be converted to a JSON string for easy setting saving
    /// </summary>
    public class UserSettings
    {
        /// <summary>
        /// The <see cref="Size"/> that the Application Window should be set to when starting.
        /// </summary>
        public Size WindowSize { get; set; } = new Size(1234, 810);

    }
}
