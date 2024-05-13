using System.Collections.Generic;

namespace Nefta.Core.Events
{
    public enum ResourceCategory
    {
        Undefined,
        SoftCurrency,
        PremiumCurrency,
        Resource,
        Consumable,
        CosmeticItem,
        CoreItem,
        Chest,
        Experience,
        Other
    }

    public abstract class ResourceEvent : GameEvent
    {
        private static readonly Dictionary<ResourceCategory, string> CategoryToString = new Dictionary<ResourceCategory, string>()
        {
            { ResourceCategory.Undefined, null },
            { ResourceCategory.SoftCurrency, "soft_currency" },
            { ResourceCategory.PremiumCurrency, "premium_currency" },
            { ResourceCategory.Resource, "resource" },
            { ResourceCategory.Consumable, "consumable" },
            { ResourceCategory.CosmeticItem, "cosmetic_item" },
            { ResourceCategory.CoreItem, "core_item" },
            { ResourceCategory.Chest, "chest" },
            { ResourceCategory.Experience, "experience" },
            { ResourceCategory.Other, "other" },
        };
            
        /// <summary>
        /// The category of the resource
        /// </summary>
        public ResourceCategory _resourceCategory;
        
        internal override string _category => CategoryToString[_resourceCategory];
    }
}