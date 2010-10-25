using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Mindscape.LightSpeed.Ria
{
    /// <summary>
    /// Class that provides the basic metadata interface to a LINQ To SQL data context.
    /// </summary>
    internal class LightSpeedTypeDescriptionContext
    {
        private LightSpeedDataModelProvider _metaModel;
        private Dictionary<string, string> _associationNameMap = new Dictionary<string, string>();

        /// <summary>
        /// Constructor that creates a metadata context for the specified LINQ To SQL domain service type
        /// </summary>
        public LightSpeedTypeDescriptionContext(Type unitOfWorkType)
        {
            this._metaModel = new LightSpeedDataModelProvider(unitOfWorkType);
        }

        /// <summary>
        /// Gets the MetaModel containing the metadata
        /// </summary>
        public LightSpeedDataModelProvider MetaModel
        {
            get
            {
                return this._metaModel;
            }
        }

        /// <summary>
        /// Returns an AssociationAttribute for the specified association member
        /// </summary>
        /// <param name="member">The metadata member corresponding to the association member</param>
        /// <returns>The Association attribute</returns>
        public System.ComponentModel.DataAnnotations.AssociationAttribute CreateAssociationAttribute(LightSpeedColumnProvider member)
        {
            var metaAssociation = member.Association;

            string associationName = GetAssociationName(metaAssociation);
            string thisKey = FormatMemberList(metaAssociation.ThisKey);
            string otherKey = FormatMemberList(metaAssociation.OtherKey);

            System.ComponentModel.DataAnnotations.AssociationAttribute assocAttrib = new System.ComponentModel.DataAnnotations.AssociationAttribute(associationName, thisKey, otherKey);
            assocAttrib.IsForeignKey = metaAssociation.IsForeignKey;
            return assocAttrib;
        }

        /// <summary>
        /// Returns a unique association name for the specified MetaAssociation
        /// </summary>
        private string GetAssociationName(LightSpeedAssociationProvider metaAssociation)
        {
            lock (this._associationNameMap)
            {
                //var ltsAssociationAttribute = metaAssociation.FromColumn.EntityTypeProperty.GetCustomAttributes(typeof(AssociationAttribute), false).Single();
                var keyFormat = "{0}.{2}-{1}.{3}";
                if (metaAssociation.IsForeignKey)
                    keyFormat = "{1}.{3}-{0}.{2}";
                string key = string.Format(keyFormat, metaAssociation.FromColumn.Table.Name,
                                           metaAssociation.ToTable.Name, metaAssociation.FromColumn.Name,
                                           metaAssociation.ToColumn.Name);
                return key;
                //string associationName = null;
                //if (!this._associationNameMap.TryGetValue(key, out associationName))
                //{
                //    // names are always formatted non-FK side type name followed by FK side type name
                //    // For example, the name for both ends of the PurchaseOrder/PurchaseOrderDetail 
                //    // association will be PurchaseOrder_PurchaseOrderDetail
                //    if (metaAssociation.IsForeignKey)
                //    {
                //        associationName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", metaAssociation.ToColumn.Name, metaAssociation.FromColumn.Name);
                //    }
                //    else
                //    {
                //        associationName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", metaAssociation.FromColumn.Name, metaAssociation.ToColumn.Name);
                //    }

                //    associationName = MakeUniqueName(associationName, this._associationNameMap.Values);
                //    this._associationNameMap[key] = associationName;
                //}

                //return associationName;
            }
        }

        /// <summary>
        /// Given a suggested name and a collection of existing names, this method
        /// creates a unique name by appending a numerix suffix as required.
        /// </summary>
        /// <param name="suggested">The desired name</param>
        /// <param name="existing">Collection of existing names</param>
        /// <returns>The unique name</returns>
        private static string MakeUniqueName(string suggested, IEnumerable<string> existing)
        {
            int i = 1;
            string currSuggestion = suggested;
            while (existing.Contains(currSuggestion))
            {
                currSuggestion = suggested + i++.ToString(CultureInfo.InvariantCulture);
            }

            return currSuggestion;
        }

        /// <summary>
        /// Comma delimits the specified member name collection
        /// </summary>
        private static string FormatMemberList(IEnumerable<string> members)
        {
            string memberList = string.Empty;
            foreach (string name in members)
            {
                if (memberList.Length > 0)
                {
                    memberList += ",";
                }
                memberList += name;
            }
            return memberList;
        }
    }
}
