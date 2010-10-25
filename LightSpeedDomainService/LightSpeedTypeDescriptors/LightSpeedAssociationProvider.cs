using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.Diagnostics;

namespace Mindscape.LightSpeed.Ria
{
    public abstract class LightSpeedAssociationProvider
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected LightSpeedAssociationProvider(LightSpeedColumnProvider fromColumn, LightSpeedTableProvider toTable, LightSpeedColumnProvider toColumn)
        {
            if(fromColumn == null)
                throw new Exception("fromColumn must not be null in association");
            FromColumn = fromColumn;
            if (toTable == null)
                throw new Exception("toTable must not be null in association"); 
            ToTable = toTable;
            if (toColumn == null)
                throw new Exception("toColumn must not be null in association");
            ToColumn = toColumn;
            IsEagerLoaded = EagerLoad();
        }

        protected bool EagerLoad()
        {
            var fi = TypeUtils.GetUnderlyingField(FromColumn.Table.EntityType, FromColumn.Name);
            if(fi == null)
                return false;

            var els = from t in fi.GetCustomAttributes(false).OfType<EagerLoadAttribute>()
                      select t;

            var eager = els.Any();
            return eager;
        }


        public LightSpeedColumnProvider ToColumn { get; protected set; }

        public LightSpeedTableProvider ToTable { get; protected set; }

        public LightSpeedColumnProvider FromColumn { get; protected set; }

        public AssociationDirection Direction { get; protected set; }

        public ReadOnlyCollection<string> ForeignKeyNames { get; protected set; }

        private List<string> _thiskey;
        public IEnumerable<string> ThisKey
        {
            get
            {
                return _thiskey;
            }
            protected set
            {
                _thiskey = value.ToList();
            }
        }
        private List<string> _otherkey;
        public IEnumerable<string> OtherKey
        {
            get { return _otherkey; }
            protected set { _otherkey = value.ToList(); }
        }

        public bool IsForeignKey
        {
            get; protected set;
        }

        public bool IsEagerLoaded
        {
            get; protected set;
        }
    }

    public class LightSpeedManyToOneAssociationProvider : LightSpeedAssociationProvider
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public LightSpeedManyToOneAssociationProvider(LightSpeedColumnProvider fromColumn, LightSpeedTableProvider toTable, LightSpeedColumnProvider toColumn, List<string> foreignKeyNames)
            : base(fromColumn, toTable, toColumn)
        {
            Direction = AssociationDirection.ManyToOne;
            ForeignKeyNames = foreignKeyNames.AsReadOnly();
            ThisKey = ForeignKeyNames;
            OtherKey = new List<string> {"Id"};
            IsForeignKey = true;
        }
    }

    public class LightSpeedOneToManyAssociationProvider : LightSpeedAssociationProvider
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public LightSpeedOneToManyAssociationProvider(LightSpeedColumnProvider fromColumn, LightSpeedTableProvider toTable, LightSpeedColumnProvider toColumn)
            : base(fromColumn, toTable, toColumn)
        {
            Direction = AssociationDirection.OneToMany;
            OtherKey = ForeignKeyNames = toColumn.Association.ForeignKeyNames;
            ThisKey = new List<string> { "Id" };
        }
    }

    public class LightSpeedOneToOneIdAssociationProvider : LightSpeedAssociationProvider
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public LightSpeedOneToOneIdAssociationProvider(LightSpeedColumnProvider fromColumn, LightSpeedTableProvider toTable, LightSpeedColumnProvider toColumn)
            : base(fromColumn, toTable, toColumn)
        {
            Direction = AssociationDirection.OneToOne;
            ThisKey  = ForeignKeyNames = toColumn.Association.ForeignKeyNames;
            OtherKey = new List<string> { "Id" };
        }
    }

    public class LightSpeedOneIdToOneAssociationProvider : LightSpeedAssociationProvider
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public LightSpeedOneIdToOneAssociationProvider(LightSpeedColumnProvider fromColumn, LightSpeedTableProvider toTable, LightSpeedColumnProvider toColumn, List<string> foreignKeyNames)
            : base(fromColumn, toTable, toColumn)
        {
            Direction = AssociationDirection.OneToOne;
            ForeignKeyNames = foreignKeyNames.AsReadOnly();
            OtherKey = ForeignKeyNames;
            ThisKey = new List<string> { "Id" };
            IsForeignKey = true;
        }
    }

    public static class AssociationExtensions
    {
        internal static IEnumerable<LightSpeedColumnProvider> GetOneToManyAssociationsTo(this LightSpeedTableProvider table, Type childType)
        {
            Type childCollectionType = typeof(EntityCollection<>).MakeGenericType(childType);
            return table.Columns.Where(c => c.ColumnType == childCollectionType);
        }

        internal static IEnumerable<LightSpeedColumnProvider> GetManyToOneAssociationsTo(this LightSpeedTableProvider table, Type parentType)
        {
            var columns = table.Columns.Where(c => c.ColumnType == parentType);
            if (!columns.Any() && typeof(Entity).IsAssignableFrom(parentType.BaseType))
                return GetManyToOneAssociationsTo(table, parentType.BaseType);
            return columns;
        }

        internal static LightSpeedColumnProvider GetOneToManyAssociation(this LightSpeedTableProvider table, Type childType, PropertyInfo manyToOneAssociation)
        {
            List<LightSpeedColumnProvider> candidates = new List<LightSpeedColumnProvider>(table.GetOneToManyAssociationsTo(childType));
            switch (candidates.Count)
            {
                case 0: return null;
                case 1: return candidates[0];
                default: return FindReverseAssociation(candidates, manyToOneAssociation);
            }
        }

        internal static LightSpeedColumnProvider GetManyToOneAssociation(this LightSpeedTableProvider table, Type childType, PropertyInfo oneToManyAssociation)
        {
            List<LightSpeedColumnProvider> candidates = new List<LightSpeedColumnProvider>(table.GetManyToOneAssociationsTo(childType));
            switch (candidates.Count)
            {
                case 0: return null;
                case 1: return candidates[0];
                default: return FindReverseAssociation(candidates, oneToManyAssociation);
            }
        }

        private static LightSpeedColumnProvider FindReverseAssociation(List<LightSpeedColumnProvider> candidates, PropertyInfo association)
        {
            // The reverse assoc attribute could in theory be on either end.  But the designer now emits it at
            // both ends so we'll assume that.
            // First we need the field because that's where the reverse assoc attribute will be.  We'll assume
            // the designer naming convention (having access to the LS type model would save us work here).

            FieldInfo associationField = TypeUtils.GetUnderlyingField(association);
            if (!associationField.IsDefined(typeof(ReverseAssociationAttribute), true))
            {
                return FindReverseAssociationUsingAttributeAtOtherEnd(candidates, association);
            }
            else
            {
                ReverseAssociationAttribute rassoc = GetReverseAssociationAttribute(associationField);
                return candidates.First(c => c.EntityTypeProperty.Name == rassoc.FieldName);
            }
        }

        private static LightSpeedColumnProvider FindReverseAssociationUsingAttributeAtOtherEnd(List<LightSpeedColumnProvider> candidates, PropertyInfo association)
        {
            // Candidates already match on type, and if we get here, then:
            // (a) there was more than one candidate on type alone; and
            // (b) there was no ReverseAssociationAttribute at our end to help.
            // So we have to find a ReverseAssociationAttribute at the other end
            // to disambiguate.

            Debug.Assert(candidates.Count > 1);
            foreach (LightSpeedColumnProvider candidate in candidates)
            {
                FieldInfo candidateField = TypeUtils.GetUnderlyingField(candidate.EntityTypeProperty);
                if (candidateField.IsDefined(typeof(ReverseAssociationAttribute), true))
                {
                    ReverseAssociationAttribute rassoc = GetReverseAssociationAttribute(candidateField);
                    if (rassoc.FieldName == association.Name)
                    {
                        return candidate;
                    }
                }
            }

            // Shouldn't happen because the LightSpeed type model would have
            // choked on this situation before we ever got here.
            throw new LightSpeedException(UnableToDetermineReverseAssociationErrorMessage(association));
        }

        private static ReverseAssociationAttribute GetReverseAssociationAttribute(FieldInfo associationField)
        {
            return (ReverseAssociationAttribute)(associationField.GetCustomAttributes(typeof(ReverseAssociationAttribute), true)[0]);
        }

        private static string UnableToDetermineReverseAssociationErrorMessage(PropertyInfo association)
        {
            return String.Format(CultureInfo.InvariantCulture, "Unable to determine reverse association of {0}.{1}", association.DeclaringType.FullName, association.Name);
        }
    }

    public enum AssociationDirection
    {
        OneToOne,
        OneToMany,
        ManyToOne
    }
}
