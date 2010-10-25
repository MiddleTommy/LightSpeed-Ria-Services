//Transformed
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.ServiceModel.DomainServices.Server;

namespace Mindscape.LightSpeed.Ria
{
    /// <summary>
    /// TypeDescriptionProvider for LINQ To SQL
    /// </summary>
    internal class LightSpeedTypeDescriptionProvider : DomainServiceDescriptionProvider
    {
        private static Dictionary<Type, LightSpeedTypeDescriptionContext> _tdpContextMap = new Dictionary<Type, LightSpeedTypeDescriptionContext>();
        private LightSpeedTypeDescriptionContext _typeDescriptionContext;

        /// <summary>
        /// Constructor that accepts a metadata context to use when generating custom type descriptors
        /// </summary>
        /// <param name="existingProvider">The parent TDP instance</param>
        /// <param name="domainServiceType">The DomainService Type exposing the entity Types this provider will be registered for</param>
        /// <param name="unitOfWorkType">The DataContext Type that exposes the Types this provider will be registered for</param>
        public LightSpeedTypeDescriptionProvider(DomainServiceDescriptionProvider existingProvider, Type domainServiceType, Type unitOfWorkType)
            : base(domainServiceType,existingProvider)
        {
            lock (_tdpContextMap)
            {
                if (!_tdpContextMap.TryGetValue(domainServiceType, out this._typeDescriptionContext))
                {
                    this._typeDescriptionContext = new LightSpeedTypeDescriptionContext(unitOfWorkType);
                    _tdpContextMap.Add(domainServiceType, _typeDescriptionContext);
                }
            }
        }

        /// <summary>
        /// Returns a custom type descriptor for the specified entity type
        /// </summary>
        /// <param name="objectType">Type of object for which we need the descriptor</param>
        /// <param name="instance">Instance of that object (alternate way to ask for information)</param>
        /// <returns>a custom type descriptor for the specified entity type</returns>
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, ICustomTypeDescriptor parent)
        {
            Type entityType = objectType;
            var metaType = _typeDescriptionContext.MetaModel.Tables.Where(t => t.Name == entityType.Name).FirstOrDefault();
            if (metaType != null)
            {
                return new LightSpeedTypeDescriptor(this._typeDescriptionContext, metaType, base.GetTypeDescriptor(objectType, parent));
            }
            else
            {
                return base.GetTypeDescriptor(objectType, parent);
            }
        }
    }
}
