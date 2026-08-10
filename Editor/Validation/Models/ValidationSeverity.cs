namespace Theoden.Editor.Validation
{
    /// <summary>
    /// Defines the severity of an issue found during THEODEN project validation.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Indicates a non-blocking problem that should be reviewed by the user.
        /// </summary>
        Warning,

        /// <summary>
        /// Indicates a problem that prevents the project from being built safely.
        /// </summary>
        Error
    }
}