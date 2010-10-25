//Transformed
using System;
using System.ComponentModel;
using System.Globalization;
using System.ServiceModel.DomainServices.Server;

namespace Mindscape.LightSpeed.Ria
{
    /// <summary>
    /// Attribute applied to a <see cref="DomainService"/> that exposes LINQ to SQL mapped
    /// Types.
    /// </summary>
     [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class LightSpeedDomainServiceDescriptionProviderAttribute : DomainServiceDescriptionProviderAttribute
    {
        private Type _unitOfWorkType;

        /// <summary>
        /// Default constructor. Using this constructor, the Type of the LINQ to SQL
        /// DataContext will be inferred from the <see cref="DomainService"/> the
        /// attribute is applied to.
        /// </summary>
        public LightSpeedDomainServiceDescriptionProviderAttribute()
            : base(typeof(LightSpeedTypeDescriptionProvider))
        {
        }

        /// <summary>
        /// The LINQ to SQL DataContext Type.
        /// </summary>
        public Type UnitOfWorkType
        {
            get
            {
                return this._unitOfWorkType;
            }
        }

        /// <summary>
        /// This method creates an instance of the <see cref="TypeDescriptionProvider"/>.
        /// </summary>
        /// <param name="existingProvider">The existing <see cref="TypeDescriptionProvider"/> for the Type the returned
        /// provider will be registered for.</param>
        /// <param name="domainServiceType">The <see cref="DomainService"/> Type metadata is being provided for.</param>
        /// <returns>The <see cref="TypeDescriptionProvider"/> instance.</returns>
        public override DomainServiceDescriptionProvider CreateProvider(Type domainServiceType, DomainServiceDescriptionProvider existingProvider)
        {
            if (domainServiceType == null)
            {
                throw new ArgumentNullException("domainServiceType");
            }

            if (this._unitOfWorkType == null)
            {
                this._unitOfWorkType = GetUnitOfWorkType(domainServiceType);
            }

            return new LightSpeedTypeDescriptionProvider(existingProvider, domainServiceType, this._unitOfWorkType);
        }

        /// <summary>
        /// Extracts the UnitOfWork type from the specified <paramref name="domainServiceType"/>.
        /// </summary>
        /// <param name="domainServiceType">A LightSpeed domain service type.</param>
        /// <returns>The type of the UnitOfWork.</returns>
        private static Type GetUnitOfWorkType(Type domainServiceType)
        {
            Type lsDomainServiceType = domainServiceType.BaseType;
            while (!lsDomainServiceType.IsGenericType || lsDomainServiceType.GetGenericTypeDefinition() != typeof(LightSpeedDomainService<>))
            {
                if (lsDomainServiceType == typeof(object))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                        @"'{0}' cannot be applied to DomainService Type '{1}' because '{1}' does not derive from '{2}'.",
                        typeof(LightSpeedDomainServiceDescriptionProviderAttribute).Name, domainServiceType.Name, typeof(LightSpeedDomainService<>).Name));
                }
                lsDomainServiceType = lsDomainServiceType.BaseType;
            }

            return lsDomainServiceType.GetGenericArguments()[0];
        }
    }
}
