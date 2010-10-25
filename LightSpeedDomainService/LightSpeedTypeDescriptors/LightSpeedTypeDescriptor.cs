using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Runtime.Serialization;
using System.Linq;
using System.ServiceModel.DomainServices.Server;

namespace Mindscape.LightSpeed.Ria
{
    /// <summary>
    /// CustomTypeDescriptor for LINQ To SQL entities
    /// </summary>
    internal class LightSpeedTypeDescriptor : CustomTypeDescriptor
    {
        private LightSpeedTypeDescriptionContext _typeDescriptionContext;
        private LightSpeedTableProvider _metaTable;

        /// <summary>
        /// Constructor that takes the metadata context, a metadata type and a parent custom type descriptor
        /// </summary>
        /// <param name="typeDescriptionContext"></param>
        /// <param name="metaType"></param>
        /// <param name="parent"></param>
        public LightSpeedTypeDescriptor(LightSpeedTypeDescriptionContext typeDescriptionContext, LightSpeedTableProvider metaTable, ICustomTypeDescriptor parent)
            : base(parent)
        {
            this._typeDescriptionContext = typeDescriptionContext;
            this._metaTable = metaTable;
        }

        /// <summary>
        /// Gets the metadata context
        /// </summary>
        public LightSpeedTypeDescriptionContext TypeDescriptionContext
        {
            get
            {
                return this._typeDescriptionContext;
            }
        }

        /// <summary>
        /// Override of the <see cref="CustomTypeDescriptor.GetProperties()"/> to obtain the list
        /// of properties for this type.
        /// </summary>
        /// <remarks>
        /// This method is overridden so that it can merge this class's parent attributes with those
        /// it infers from the DAL-specific attributes.
        /// </remarks>
        /// <returns>list of properties for this type</returns>
        public override PropertyDescriptorCollection GetProperties()
        {
            // Get properties from our parent
            PropertyDescriptorCollection originalCollection = base.GetProperties();

            //bool customDescriptorsCreated = false;
            List<PropertyDescriptor> tempPropertyDescriptors = new List<PropertyDescriptor>();

            // for every property exposed by our parent, see if we have additional metadata to add
            foreach (PropertyDescriptor propDescriptor in originalCollection)
            {
                string name = propDescriptor.Name;
                var column = this._metaTable.Columns.Where(t => t.Name == name).FirstOrDefault();
                if (column != null)
                {
                    Attribute[] newMetadata = this.GetEntityMemberAttributes(column, propDescriptor).ToArray();
                    tempPropertyDescriptors.Add(new MetadataPropertyDescriptorWrapper(propDescriptor, newMetadata));
                }
                
            }
            return new PropertyDescriptorCollection(tempPropertyDescriptors.ToArray(), true);
        }

        /// <summary>
        /// Returns a collection of all the <see cref="Attribute"/>s we infer from the metadata associated
        /// with the metadata member corresponding to the given property descriptor
        /// </summary>
        /// <param name="propertyDescriptor">A <see cref="PropertyDescriptor"/>.</param>
        /// <returns>a collection of attributes inferred from metadata in the given descriptor</returns>
        private IEnumerable<Attribute> GetEntityMemberAttributes(LightSpeedColumnProvider column, PropertyDescriptor propertyDescriptor)
        {
            List<Attribute> attributes = new List<Attribute>();
            //MetaDataMember member = this._metaType.DataMembers.Where(p => p.Name == propertyDescriptor.Name).SingleOrDefault();
            if (column != null)
            {
                if (column.IsPrimaryKey && propertyDescriptor.Attributes[typeof(KeyAttribute)] == null)
                {
                    attributes.Add(new KeyAttribute());
                }

                if (column.Association != null)
                {
                    if (propertyDescriptor.Attributes[typeof(System.ComponentModel.DataAnnotations.AssociationAttribute)] == null)
                    {
                        var assocAttrib =
                            this.TypeDescriptionContext.CreateAssociationAttribute(column);
                        attributes.Add(assocAttrib);
                    }
                    if(column.Association.IsEagerLoaded && propertyDescriptor.Attributes[typeof(IncludeAttribute)] == null)
                    {
                        attributes.Add(new IncludeAttribute());
                    }
                }

                if (column.IsExcluded && propertyDescriptor.Attributes[typeof(ExcludeAttribute)] == null)
                {
                    attributes.Add(new ExcludeAttribute());
                }
                else
                {
                    attributes.Add(new DataMemberAttribute());
                }

                //if (column.UpdateCheck != UpdateCheck.Never &&
                //    propertyDescriptor.Attributes[typeof(ConcurrencyCheckAttribute)] == null)
                //{
                //    attributes.Add(new ConcurrencyCheckAttribute());
                //}

                //if (column.IsVersion &&
                //    propertyDescriptor.Attributes[typeof(TimestampAttribute)] == null)
                //{
                //    attributes.Add(new TimestampAttribute());
                //}

                bool isStringType = propertyDescriptor.PropertyType == typeof(string) || propertyDescriptor.PropertyType == typeof(char[]);
                if (isStringType && column.ColumnType != null && column.MaxLength > 0 &&
                    propertyDescriptor.Attributes[typeof(StringLengthAttribute)] == null)
                {
                    attributes.Add(new StringLengthAttribute(column.MaxLength));
                }
            }
            return attributes.ToArray();
        }

        ///// <summary>
        ///// Parse the DbType to determine whether a StringLengthAttribute should be added.
        ///// </summary>
        ///// <param name="dbType">The DbType from the MetaDataMember.</param>
        ///// <param name="attributes">The list of attributes to append to.</param>
        //internal static void InferStringLengthAttribute(string dbType, List<Attribute> attributes)
        //{
        //    if (dbType == null || dbType.Length <= 0)
        //    {
        //        return;
        //    }

        //    // we can assume that the SqlType if specified will be the first part of the string,
        //    // so the string will be of the form "NVarChar(80)", "char(15)", etc.
        //    string sqlType = dbType.Trim();
        //    int i = sqlType.IndexOf(' ');
        //    if (i != -1)
        //    {
        //        sqlType = sqlType.Substring(0, i);
        //    }
        //    i = sqlType.IndexOf("char(", StringComparison.OrdinalIgnoreCase);
        //    if (i != -1)
        //    {
        //        i += 5;
        //        int j = sqlType.IndexOf(")", i, StringComparison.Ordinal);
        //        if (j != -1)
        //        {
        //            // if the portion between the parenthesis is integral
        //            // add the attribute. Note that "VarChar(max)" will be
        //            // skipped.
        //            string stringLen = sqlType.Substring(i, j - i);
        //            int maxLength;
        //            if (int.TryParse(stringLen, NumberStyles.Integer, CultureInfo.InvariantCulture, out maxLength))
        //            {
        //                attributes.Add(new StringLengthAttribute(maxLength));
        //            }
        //        }
        //    }
        //}

        internal class MetadataPropertyDescriptorWrapper : PropertyDescriptor
        {
            private PropertyDescriptor _descriptor;
            public MetadataPropertyDescriptorWrapper(PropertyDescriptor descriptor, Attribute[] attrs)
                : base(descriptor, attrs)
            {
                _descriptor = descriptor;
            }

            public override void AddValueChanged(object component, EventHandler handler)
            {
                _descriptor.AddValueChanged(component, handler);
            }

            public override bool CanResetValue(object component)
            {
                return _descriptor.CanResetValue(component);
            }

            public override Type ComponentType
            {
                get
                {
                    return _descriptor.ComponentType;
                }
            }

            public override object GetValue(object component)
            {
                return _descriptor.GetValue(component);
            }

            public override bool IsReadOnly
            {
                get
                {
                    return _descriptor.IsReadOnly;
                }
            }

            public override Type PropertyType
            {
                get
                {
                    return _descriptor.PropertyType;
                }
            }

            public override void RemoveValueChanged(object component, EventHandler handler)
            {
                _descriptor.RemoveValueChanged(component, handler);
            }

            public override void ResetValue(object component)
            {
                _descriptor.ResetValue(component);
            }

            public override void SetValue(object component, object value)
            {
                _descriptor.SetValue(component, value);
            }

            public override bool ShouldSerializeValue(object component)
            {
                return _descriptor.ShouldSerializeValue(component);
            }

            public override bool SupportsChangeEvents
            {
                get
                {
                    return _descriptor.SupportsChangeEvents;
                }
            }
        }
    }

    public class ExcludePropertyDescriptor:PropertyDescriptor
    {
        private PropertyDescriptor desc;
        public ExcludePropertyDescriptor(PropertyDescriptor descriptor):base(descriptor)
        {
            desc = descriptor;
        }

   
        public override bool  CanResetValue(object component)
        {
            return desc.CanResetValue(component);
        }

        public override Type  ComponentType
        {
            get { return desc.ComponentType; }
        }

        public override object  GetValue(object component)
        {
            return desc.GetValue(component);
        }

        public override bool  IsReadOnly
        {
            get { return desc.IsReadOnly; }
        }

        public override Type  PropertyType
        {
            get { return desc.PropertyType; }
        }

        public override void  ResetValue(object component)
        {
            desc.ResetValue(component);
        }

        public override void  SetValue(object component, object value)
        {
 	        desc.SetValue(component,value);
        }

        public override bool  ShouldSerializeValue(object component)
        {
            return false;
        }
    }

    
}
