using Mindscape.LightSpeed;
using Mindscape.LightSpeed.Linq;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ServiceModel.DomainServices.Server;
using Mindscape.LightSpeed.Ria;
using System.Collections.Generic;
using Mindscape.LightSpeed.Data;
using System;

namespace SilverCards.Web
{
    [EnableClientAccess()]
    public partial class ExampleService : LightSpeedDomainService<MyCardsUnitOfWork>
    {
        internal const string connection = "ConnectionString";
        internal const DataProvider provider = DataProvider.SqlServer2008;

        public void UpdateDrawing(Drawing dwg)
        {
            //add custom update logic if needed.
            if (dwg.ExtraId == 0)
                Delete(dwg);//delete drawing because its parent is deleted
            else
                Update(dwg);
        }

        public IQueryable<Drawing> Drawings()
        {
            return NewUnitOfWork.Drawings;
        }

        public void InsertDrawing(Drawing dwg)
        {
            //Queue the dwg for insert
            Insert(dwg);
        }

        public void DeleteDrawing(Drawing dwg)
        {
            //queue the dwg for insert
            Delete(dwg);
        }

        protected override LightSpeedContext<MyCardsUnitOfWork> CreateDataContext()
        {
            //Get Context is a helper method that returns a Context with the Default Ria Services settings.
            return GetContext(connection, provider);
        }

    }

    //Example Exitity
    public class Drawing:Entity<long>
    {
        //fake relation entity id
        public long ExtraId { get; set; }

    }


    //Example Unit of Work
    public partial class MyCardsUnitOfWork : Mindscape.LightSpeed.UnitOfWork
    {

        public System.Linq.IQueryable<Drawing> Drawings
        {
          get { return this.Query<Drawing>(); }
        }
    }
}


