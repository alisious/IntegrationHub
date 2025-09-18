using System;
using System.Collections.Generic;
using System.Text;
using WebCon.WorkFlow.SDK.ActionPlugins.Model;

namespace Trentum.Horkos.BpsExtensions
{
    public static class ActionParamsHelpers
    {
        public static bool GetParamBoolOrDefaultValue(RunCustomActionParams args, int? paramFieldId)
        {
            if (!paramFieldId.HasValue)
            {
                return true;
            }

            var field = args.Context.CurrentDocument.BooleanFields.GetByID(paramFieldId.Value);
            if (field != null && field.Value.HasValue)
            {
                return field.Value.Value;
            }
            else
                return true;

        }
    }
}

