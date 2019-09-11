using CoreNetCore.Utils;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;

namespace CoreNetCore.Configuration
{
    public abstract class CfgSectionBase
    {
        public List<ValidationResult> ValidateErrors = new List<ValidationResult>();

        public List<string> ErrorMessages
        {
            get
            {
                return ValidateErrors.Select(item => item.ErrorMessage).ToList();
            }
        }

        public virtual bool Validate()
        {
            var context = new ValidationContext(this, serviceProvider: null, items: null);
            if (ValidateErrors == null)
            {
                ValidateErrors = new List<ValidationResult>();
            }
            var res = Validator.TryValidateObject(
                this, context, ValidateErrors,
                validateAllProperties: true
            );
            return res;
        }

        public virtual  void ValidateAndTrace(string sectionName)
        {
            if (!Validate())
            {
                TraceErrosAndGenerateException(sectionName);
            }
        }

        public virtual bool ValidateChild(CfgSectionBase child)
        {
            if (child != null)
            {
                var res = child.Validate();
                ValidateErrors.AddRange(child.ValidateErrors);
                return res;
            }
            return true;
        }

        public virtual void TraceErrosAndGenerateException(string sectionName)
        {
            foreach (var err in  ErrorMessages)
            {
                Trace.TraceError(err);
            }
            if (ValidateErrors.Any())
            {
                throw new CoreException($"Config section [{sectionName}] has any errors!");
            }
        }
    }
}