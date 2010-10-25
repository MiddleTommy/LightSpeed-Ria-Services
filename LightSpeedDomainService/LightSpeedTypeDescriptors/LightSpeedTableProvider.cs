using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.ObjectModel;

namespace Mindscape.LightSpeed.Ria
{
    public class LightSpeedTableProvider
    {
        private PropertyInfo _queryProperty;
        private ReadOnlyCollection<LightSpeedColumnProvider> _providers;
        private object _initialisationLock = new object();
        private Type _entityType;
        private string _name;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public LightSpeedTableProvider(LightSpeedDataModelProvider model, PropertyInfo queryProperty)
        {
            EntityType = queryProperty.PropertyType.GetGenericArguments()[0];
            Name = EntityType.Name;
            _queryProperty = queryProperty;
            DataModel = model;
        }

        public LightSpeedDataModelProvider DataModel { get; private set; }

        public ReadOnlyCollection<LightSpeedColumnProvider> Columns
        {
            get
            {
                if (_providers == null)
                {
                    lock (_initialisationLock)
                    {
                        if (_providers == null)
                        {
                            List<LightSpeedColumnProvider> providers = new List<LightSpeedColumnProvider>();
                            var props = EntityType.GetProperties();
                            foreach (PropertyInfo property in props)
                            {
                                if (property.GetIndexParameters().Length == 0)
                                {
                                    LightSpeedColumnProvider provider = new LightSpeedColumnProvider(this, property);
                                    providers.Add(provider);
                                }
                            }
                            _providers = providers.AsReadOnly();
                        }
                    }
                }

                return _providers;
            }
        }

        public Type EntityType
        {
            get { return _entityType; }
            set { _entityType = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public IQueryable GetQuery(object context)
        {
            return (IQueryable)(_queryProperty.GetValue(context, null));
        }



        //public object EvaluateForeignKey(object row, string foreignKeyName)
        //{
        //  // Dynamic data wants foreign keys for everything.  But the non-FK end of a one-to-one assoc
        //  // does not have a FK field.  The only way to get the foreign key is to load the
        //  // association and get the ID of the guy at the other end.

        //  if (TypeUtils.IsSyntheticKey(foreignKeyName))
        //  {
        //    PropertyInfo otoAssociation = TypeUtils.GetAssociation(row, foreignKeyName);
        //    if (otoAssociation == null)
        //    {
        //      return null;
        //    }

        //    Entity target = otoAssociation.GetValue(row, null) as Entity;

        //    return TypeUtils.GetId(target);
        //  }

        //  return base.EvaluateForeignKey(row, foreignKeyName);
        //}
    }
}
