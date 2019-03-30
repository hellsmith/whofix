using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D365Api
{
    public static class Extentions
    {
        public static T GetAliasedValue<T>(this Entity en, string attribute)
        {
            var objAliasedValue = en.GetAttributeValue<AliasedValue>(attribute);

            if (objAliasedValue != null && objAliasedValue.Value is T)
            {
                return (T)objAliasedValue.Value;
            }
            return default(T);
        }
    }
}
