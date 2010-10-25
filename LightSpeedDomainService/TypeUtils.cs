using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;

namespace Mindscape.LightSpeed.Ria
{
  internal static class TypeUtils
  {
    internal static FieldInfo GetUnderlyingField(PropertyInfo property)
    {
      return GetUnderlyingField(property.DeclaringType, property.Name);
    }

    internal static FieldInfo GetUnderlyingField(Type type, string propertyName)
    {
      string fieldName = "_" + Char.ToLower(propertyName[0], CultureInfo.InvariantCulture) + propertyName.Substring(1);
      FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
      return field;
    }

    private const string SyntheticKeyPrefix = "*";
    //private const string SyntheticKeyPrefix = "";

    internal static string CreateSyntheticKey(string oneToOneAssociationName)
    {
      return SyntheticKeyPrefix + oneToOneAssociationName;
    }

    internal static bool IsSyntheticKey(string foreignKeyName)
    {
      return foreignKeyName.StartsWith(SyntheticKeyPrefix, StringComparison.OrdinalIgnoreCase);
    }

    internal static string GetAssociationName(string syntheticKey)
    {
      Debug.Assert(IsSyntheticKey(syntheticKey));
      return syntheticKey.Substring(SyntheticKeyPrefix.Length);
    }

    internal static PropertyInfo GetAssociation(object entity, string syntheticKey)
    {
      Debug.Assert(IsSyntheticKey(syntheticKey));
      string associationName = GetAssociationName(syntheticKey);
      return entity.GetType().GetProperty(associationName);
    }

    internal static object GetId(Entity entity)
    {
      if (entity == null)
      {
        return null;
      }

      return entity.GetType().InvokeMember("Id", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public, null, entity, null, CultureInfo.InvariantCulture);
    }
  }
}
