using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel.DomainServices.Server;
using Mindscape.LightSpeed.Logging;
using Mindscape.LightSpeed;
using Mindscape.LightSpeed.Ria;
using ChangeSet = System.ServiceModel.DomainServices.Server.ChangeSet;

namespace Mindscape.LightSpeed.Ria
{
    /// <summary>
    /// Base class for DomainServices operating on LINQ To SQL data models
    /// </summary>
    /// <typeparam name="TContext">Type of DomainContext to instantiate the LinqToSqlDomainService with</typeparam>
    [LightSpeedDomainServiceDescriptionProvider]
    public abstract class LightSpeedDomainService : DomainService
    {

        protected LightSpeedContext ctx;

        protected override int Count<T>(IQueryable<T> query)
        {
            return query.Count<T>();
        }

        protected override bool PersistChangeSet()
        {
            return this.InvokeSubmitChanges(true);
        }

        protected bool ChangeSetContains(params Type[] types)
        {
            foreach (var type in types)
            {
                if (this.ChangeSet.ChangeSetEntries.Any(e => e.Entity.GetType() == type))
                    return true;
            }
            return false;
        }

        protected abstract void LoadDataContext();
        protected readonly List<Entity> NewEntities = new List<Entity>();
        protected readonly List<Entity> ModifiedEntities = new List<Entity>();
        protected readonly List<Entity> DeletedEntities = new List<Entity>();

        private bool InvokeSubmitChanges(bool retryOnConflict)
        {
            try
            {
                if (ctx == null)
                    LoadDataContext();
                var work = this.ctx.CreateUnitOfWork();
                foreach (var ent in NewEntities)
                {
                    if (!DeletedEntities.Contains(ent))
                        work.Add(ent);
                }
                foreach (var ent in ModifiedEntities)
                {
                    work.Attach(ent);
                }
                foreach (var ent in DeletedEntities)
                {
                    if (!NewEntities.Contains(ent))
                    {
                        //work.Attach(ent);
                        if(ent.EntityState == EntityState.New)
                            ent.SetEntityState(EntityState.Default);
                        work.Remove(ent);
                    }
                }
                work.SaveChanges();
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        protected void Delete(Entity ent)
        {
            DeletedEntities.Add(ent);
        }

        protected void Update(Entity ent)
        {
            ent.SetEntityState(EntityState.Modified);
            ModifiedEntities.Add(ent);
        }

        protected void Insert(Entity ent)
        {
            NewEntities.Add(ent);
        }

       
    }

    public abstract class LightSpeedDomainService<TUnitOfWork> : LightSpeedDomainService where TUnitOfWork : UnitOfWork, new()
    {
        private LightSpeedContext<TUnitOfWork> _dataContext;

        /// <summary>
        /// Gets the DataContext for this service
        /// </summary>
        /// <value>This property always gets the current DataContext.  If it has not yet been created,
        /// it will create one.
        /// </value>
        protected LightSpeedContext<TUnitOfWork> DataContext
        {
            get
            {
                if (this._dataContext == null)
                {
                    LoadDataContext();
                }
                return this._dataContext;
            }
        }



        protected virtual TUnitOfWork NewUnitOfWork
        {
            get
            {
                var work = DataContext.CreateUnitOfWork();
                work.LazyLoadOff = true;
                //work.ConnectionStrategy = new AzureConnectionStrategy(DataContext);
                return work;
            }
        }

        protected virtual TUnitOfWork NewLazyUnitOfWork
        {
            get
            {
                var work = DataContext.CreateUnitOfWork();
                work.LazyLoadOff = false;
                return work;
            }
        }

        protected override void LoadDataContext()
        {
            ctx = this._dataContext = this.CreateDataContext();
        }

        /// <summary>
        /// Creates and returns the DataContext instance that will be used by this service.
        /// </summary>
        /// <returns>The DomainContext</returns>
        protected abstract LightSpeedContext<TUnitOfWork> CreateDataContext();

       /// <summary>
       /// Get Context is a helper method that returns a Context with the Default Ria Services settings.
       /// Modify this method to save you repetative settings of LightSpeedContext creation.
       /// </summary>
       /// <param name="connection"></param>
       /// <param name="provider"></param>
       /// <returns></returns>
        public static LightSpeedContext<TUnitOfWork> GetContext(string connection, DataProvider provider)
        {
            var nctx = new LightSpeedContext<TUnitOfWork>();
            nctx.ConnectionString = connection;
            nctx.DataProvider = provider;
            nctx.IdentityMethod = IdentityMethod.KeyTable;
#if DEBUG
            nctx.Logger = new TraceLogger();
#endif
            //nctx.Cache = new CacheBroker(new DefaultCache());
            return nctx;
        }
    }
}
