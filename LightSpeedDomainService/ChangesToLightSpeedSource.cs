using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mindscape.LightSpeed;

namespace LightSpeedDomainService.Changes
{
    public class Entity<TId> 
    {
        private TId _id;
        //Add Set Method to Id Property
        public TId Id
        {
           set
            {
               //only set id if it is not already set.
                if (Equals(_id, default(TId)))
                {
                    _id = value;
                    OnPropertyChanged("Id");
                }
            }
        }        

    }

    public class UnitOfWorkBase
    {
        //Add the ability to turn Lazy Loading Off for EagerLoading Aggregates compatablity with IncludeAttribute 
        //Lazy Loading off is the Default Behavior for LightSpeedDomainService.
        private bool _lazyLoadOff = false;

        /// <summary>
        /// Turns off Lazy Loading
        /// </summary>
        public bool LazyLoadOff
        {
            get { return _lazyLoadOff; }
            set { _lazyLoadOff = value; }
        }
    }

    public abstract class Entity
    {
        //Add the LazyLoading Check in the Entity
        private void GetInternal(IEntityHolder entityHolder, Type entityType)
        {
            if (/*MindscapeCide*/true)
            {
                //mindscape code
            }
            //Change this line
            if ((UnitOfWork != null && !UnitOfWork.LazyLoadOff) && entityHolder.IsLazy)
            {
                //Mindscape Code
            }
        }

        private void LoadCollection(IEntityCollection entityCollection, Entity parent)
        {
            if (/*Mindscape Code*/true)
            {
                //change this line
                if (entityCollection.IsLazy && (EntityState != EntityState.New) && (UnitOfWork != null) && !UnitOfWork.LazyLoadOff)
                {
                    //mindscape code
                }
            }
            else
            {
                //mindscape code
            }
        }
    }
}
