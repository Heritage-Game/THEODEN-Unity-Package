using System;
using UnityEngine;

namespace Theoden.Editor.Validation
{
    /// <summary>
    /// Executes all validation rules required to verify a THEODEN project.
    /// </summary>
    public static class TheodenProjectValidator
    {
        /// <summary>
        /// Validation rules executed in their required order.
        /// </summary>
        private static readonly ITheodenValidationRule[] Rules =
        {
            new ProjectConfigurationValidationRule(),
            new ProjectStructureValidationRule(),
            new LocalizedContentValidationRule(),
            new ContentReferenceValidationRule(),
            new AddressablesValidationRule()
        };

        /// <summary>
        /// Validates the selected THEODEN project.
        /// </summary>
        /// <param name="context">
        /// The context of the project selected as the validation target.
        /// </param>
        /// <returns>
        /// A report containing all errors and warnings found by the
        /// registered validation rules.
        /// </returns>
        public static TheodenValidationReport Validate(
            TheodenProjectContext context)
        {
            TheodenValidationReport report = new();

            foreach (ITheodenValidationRule rule in Rules)
            {
                ExecuteRule(
                    rule,
                    context,
                    report
                );
            }

            return report;
        }

        /// <summary>
        /// Executes one validation rule and converts unexpected exceptions
        /// into blocking validation errors.
        /// </summary>
        private static void ExecuteRule(
            ITheodenValidationRule rule,
            TheodenProjectContext context,
            TheodenValidationReport report)
        {
            if (rule == null)
                return;

            try
            {
                rule.Validate(context, report);
            }
            catch (Exception exception)
            {
                string ruleName =
                    rule.GetType().Name;

                report.AddError(
                    "VALIDATION_RULE_FAILED",
                    $"The validation rule '{ruleName}' could not " +
                    $"complete because of an unexpected error: " +
                    exception.Message,
                    context?.projectFolderPath ?? ""
                );

                Debug.LogException(exception);
            }
        }
    }
}