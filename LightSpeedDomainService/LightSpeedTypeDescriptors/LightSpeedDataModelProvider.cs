//transformed
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Web;
using Mindscape.LightSpeed.Querying;

namespace Mindscape.LightSpeed.Ria
{
  public class LightSpeedDataModelProvider
  {
    //private readonly LightSpeedContext _context;
    private readonly ReadOnlyCollection<LightSpeedTableProvider> _providers;
      private readonly Type _unitOfWorkType;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public LightSpeedDataModelProvider(Type unitOfWorkModelType)
    {
        _unitOfWorkType = unitOfWorkModelType;
      //_context = context;

      List<LightSpeedTableProvider> providers = new List<LightSpeedTableProvider>();

      foreach (PropertyInfo property in UnitOfWorkType.GetProperties())
      {
        Type propertyType = property.PropertyType;
        if (propertyType.IsGenericType && typeof(IQueryable).IsAssignableFrom(propertyType))
        {
          LightSpeedTableProvider t = new LightSpeedTableProvider(this, property);
          providers.Add(t);
        }
      }

      _providers = providers.AsReadOnly();
    }

      private Type UnitOfWorkType
      {
          get
          {
              return _unitOfWorkType;
          }
      }

   public ReadOnlyCollection<LightSpeedTableProvider> Tables
    {
      get { return _providers; }
    }
  }

  public class LightSpeedDataModelProvider<TUnitOfWork> : LightSpeedDataModelProvider
    where TUnitOfWork : IUnitOfWork, new()
  {
    public LightSpeedDataModelProvider(LightSpeedContext<TUnitOfWork> context)
      : base(typeof(TUnitOfWork))
    {
    }
  }
}
