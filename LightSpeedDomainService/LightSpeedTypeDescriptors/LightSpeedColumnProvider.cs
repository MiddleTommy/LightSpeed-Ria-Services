using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Mindscape.LightSpeed.Validation;

namespace Mindscape.LightSpeed.Ria
{
    [System.Diagnostics.DebuggerDisplay("LightSpeedColumnProvider Name={Name}")]
    public class LightSpeedColumnProvider
    {
        private static readonly string[] LightSpeedSpecialProperties = new string[]
      {
        "IsValid",
        "Error",
        "Errors",
        "EntityState",
        "Item",

        // TODO: do these need to take account of the context's naming strategy?
        "LockVersion",
        "DeletedOn",
        "CreatedOn",
        "UpdatedOn"
      };

        private static readonly string[] LightSpeedExcludeProperties = new string[]
      {
        "IsValid",
        "Error",
        "Errors",
        "EntityState",
        "Item",
        "ChangeTracker"
      };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public LightSpeedColumnProvider(LightSpeedTableProvider tableProvider, PropertyInfo property)
        {
            Name = property.Name;
            EntityTypeProperty = property;
            ColumnType = property.PropertyType;
            IsPrimaryKey = (property.Name == "Id"); //TODO: add support for composite keys
            IsGenerated = IsPrimaryKey || LightSpeedSpecialProperties.Contains(property.Name);
            IsCustomProperty = LightSpeedSpecialProperties.Contains(property.Name);
            IsSortable = !IsAssociationType(property.PropertyType);
            IsExcluded = LightSpeedExcludeProperties.Contains(property.Name);
            Table = tableProvider;

            InitialiseMaxLength();
        }

        public LightSpeedTableProvider Table { get; private set; }

        public bool IsSortable
        {
            get;
            private set;
        }

        public bool IsExcluded
        {
            get;
            private set;
        }

        public bool IsCustomProperty
        {
            get;
            private set;
        }

        public bool IsGenerated
        {
            get;
            private set;
        }

        public bool IsPrimaryKey
        {
            get;
            private set;
        }

        public Type ColumnType
        {
            get;
            private set;
        }

        public PropertyInfo EntityTypeProperty
        {
            get;
            private set;
        }

        public string Name
        {
            get;
            private set;
        }


        private static bool IsAssociationType(Type type)
        {
            return (typeof(Entity).IsAssignableFrom(type)
              || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EntityCollection<>)));
        }

        private void InitialiseMaxLength()
        {
            FieldInfo field = TypeUtils.GetUnderlyingField(EntityTypeProperty);
            if (field != null && field.FieldType == typeof(string) && field.IsDefined(typeof(ValidateLengthAttribute), true))
            {
                ValidateLengthAttribute attr = (ValidateLengthAttribute)(field.GetCustomAttributes(typeof(ValidateLengthAttribute), true)[0]);
                if (attr.Maximum > 0)
                {
                    MaxLength = attr.Maximum;
                }
            }
        }

        public int MaxLength
        {
            get;
            private set;
        }

        private const string FKSuffix = "Id";

        public bool Nullable
        {
            get
            {
                EnsureInitialised();
                return _nullable;
            }
            protected set
            {
                _nullable = value;
            }
        }

        public bool IsForeignKeyComponent
        {
            get
            {
                EnsureInitialised();
                return _isForeignKeyComponent;
            }
            protected set
            {
                _isForeignKeyComponent = value;
            }
        }

        public LightSpeedAssociationProvider Association
        {
            get
            {
                EnsureInitialised();
                return _association;
            }
            protected set
            {
                _association = value;
            }
        }

        private bool _initialised;
        private object _initialisationLock = new object();
        private bool _nullable;
        private bool _isForeignKeyComponent;
        private LightSpeedAssociationProvider _association;

        private void EnsureInitialised()
        {
            if (!_initialised)
            {
                lock (_initialisationLock)
                {
                    if (!_initialised)
                    {
                        
                        InitialiseNullability();
                        InitialiseAssociation();
                        InitialiseIsForeignKeyComponent();
                        _initialised = true;
                        
                    }
                }
            }
        }

        private void InitialiseIsForeignKeyComponent()
        {
            // This stuff, like the entity property setter stuff, could really do with access to the LS type model
            if (Name.EndsWith(FKSuffix, StringComparison.OrdinalIgnoreCase))  // assuming designer conventions
            {
                string associationName = Name.Substring(0, Name.Length - FKSuffix.Length);
                LightSpeedColumnProvider associationColumn = Table.Columns.FirstOrDefault(c => c.Name == associationName);
                IsForeignKeyComponent = (associationColumn != null);
            }
        }

        private void InitialiseAssociation()
        {
            // This stuff, like the entity property setter stuff, could really do with access to the LS type model
            if (typeof(Entity).IsAssignableFrom(ColumnType))
            {
                // this might be a backreference or it might be either end of a one-to-one assoc

                LightSpeedTableProvider toTable = FindTableProvider(ColumnType);
                
                LightSpeedColumnProvider otmToColumn = toTable.GetOneToManyAssociation(Table.EntityType, EntityTypeProperty);
                //LightSpeedColumnProvider mtoToColumn = toTable.GetManyToOneAssociation(Table.EntityType, EntityTypeProperty);
                bool isOneToOne = (otmToColumn == null);
                string fkName = EntityTypeProperty.Name + FKSuffix; // for now -- designer conventions FTW

                if (isOneToOne)
                {
                    LightSpeedColumnProvider toColumn = toTable.GetManyToOneAssociation(Table.EntityType, EntityTypeProperty);
                    if (toColumn == null)
                        return;
                    if (toTable.Columns.Any(t => t.Name == toColumn.Name + FKSuffix))
                    {

                        fkName = toColumn.Name + FKSuffix;
                        //bool atFKEnd = Table.Columns.Any(c => c.Name.Equals(fkName, StringComparison.OrdinalIgnoreCase));

                        //if (!atFKEnd)
                        //{
                        //    // Dynamic Data insists on having a FK anyway.  We will create a synthetic key that
                        //    // doesn't correspond to any property, and which we can recognise and give special
                        //    // handling to when it comes to resolving and updating the value.
                        //    fkName = TypeUtils.CreateSyntheticKey(Name);
                        //}

                        Association = new LightSpeedOneIdToOneAssociationProvider(this, toTable, toColumn, new List<string> { fkName });
                    }
                    else
                        Association = new LightSpeedOneToOneIdAssociationProvider(this, toTable, toColumn);
                        
                }
                else
                {
                    // it's a backreference
                    Association = new LightSpeedManyToOneAssociationProvider(this, toTable, otmToColumn, new List<string> { fkName });
                }
            }
            else if (ColumnType.IsGenericType && ColumnType.GetGenericTypeDefinition() == typeof(EntityCollection<>))
            {
                // it's a child collection
                LightSpeedTableProvider toTable = FindTableProvider(ColumnType.GetGenericArguments()[0]);
                LightSpeedColumnProvider toColumn = toTable.GetManyToOneAssociation(Table.EntityType, EntityTypeProperty);
                if(toColumn == null)
                {
                    //check for inheritance

                }
                Association = new LightSpeedOneToManyAssociationProvider(this, toTable, toColumn);
            }
        }

        private void InitialiseNullability()
        {
            // This stuff, like the entity property setter stuff, could really do with access to the LS type model
            FieldInfo field = TypeUtils.GetUnderlyingField(EntityTypeProperty);
            if (field != null && field.IsDefined(typeof(ValidatePresenceAttribute), true))
            {
                Nullable = false;
                return;
            }

            if (typeof(Entity).IsAssignableFrom(ColumnType))
            {
                LightSpeedTableProvider toTable = FindTableProvider(ColumnType);
                LightSpeedColumnProvider otmToColumn = toTable.GetOneToManyAssociation(Table.EntityType, EntityTypeProperty);
                bool isOneToOne = (otmToColumn == null);

                string fkName = EntityTypeProperty.Name + FKSuffix; // for now -- designer conventions FTW
                LightSpeedColumnProvider fkColumn = Table.Columns.FirstOrDefault(c => c.Name.Equals(fkName, StringComparison.OrdinalIgnoreCase));

                if (isOneToOne && fkColumn == null)
                {
                    // We are at the non-FK end of a one-to-one association, and must rely on
                    // ValidatePresenceAttribute
                    Nullable = field == null || !field.IsDefined(typeof(ValidatePresenceAttribute), true);
                }
                else
                {
                    Nullable = (fkColumn != null && fkColumn.Nullable);
                }
            }
            else
            {
                if (EntityTypeProperty.IsDefined(typeof(ValidatePresenceAttribute), true))
                {
                    Nullable = false;
                }
                else
                {
                    Type propertyType = EntityTypeProperty.PropertyType;
                    Nullable = (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                      || propertyType == typeof(string);
                }
            }
        }

        private LightSpeedTableProvider FindTableProvider(Type type)
        {
            foreach (var table in Table.DataModel.Tables)
            {
                if (table.EntityType == type)
                {
                    return table;
                }
            }
            throw new LightSpeedException("No unit of work query property found for type " + type.FullName);
        }
    }
}
