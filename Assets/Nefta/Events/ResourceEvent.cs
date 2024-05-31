using System.Collections.Generic;

namespace Nefta.Core.Events
{
    public enum ResourceCategory
    {
        Other,
        SoftCurrency,
        PremiumCurrency,
        Resource,
        Consumable,
        CosmeticItem,
        CoreItem,
        Chest,
        Experience
    }

    public abstract class ResourceEvent : GameEvent
    {
        private static readonly Dictionary<ResourceCategory, string> CategoryToString = new Dictionary<ResourceCategory, string>()
        {
            { ResourceCategory.Other, "other" },
            { ResourceCategory.SoftCurrency, "soft_currency" },
            { ResourceCategory.PremiumCurrency, "premium_currency" },
            { ResourceCategory.Resource, "resource" },
            { ResourceCategory.Consumable, "consumable" },
            { ResourceCategory.CosmeticItem, "cosmetic_item" },
            { ResourceCategory.CoreItem, "core_item" },
            { ResourceCategory.Chest, "chest" },
            { ResourceCategory.Experience, "experience" }
        };
            
        /// <summary>
        /// The category of the resource
        /// </summary>
        public ResourceCategory _resourceCategory;
        
        internal override string _category => CategoryToString[_resourceCategory];
                        
        protected ResourceEvent(ResourceCategory category)
        {
            _resourceCategory = category;
        }

    }
}