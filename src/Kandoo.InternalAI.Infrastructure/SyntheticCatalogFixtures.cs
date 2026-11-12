using Kandoo.InternalAI.Core;

namespace Kandoo.InternalAI.Infrastructure;

public static class SyntheticCatalogFixtures
{
    public static readonly Guid PartyStoreId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid GroceryStoreId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid ClothingStoreId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static readonly DateTimeOffset ObservedAt = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    public static IReadOnlyList<StoreCatalogProfile> Profiles =>
    [
        new(PartyStoreId, "party-supply", 1, "party-schema-v1", CatalogMappingStatus.Approved, "synthetic-fixture:reviewed-v1", ObservedAt),
        new(GroceryStoreId, "grocery", 1, "grocery-schema-v3", CatalogMappingStatus.Approved, "synthetic-fixture:reviewed-v1", ObservedAt),
        new(ClothingStoreId, "clothing", 1, "clothing-schema-v2", CatalogMappingStatus.Approved, "synthetic-fixture:reviewed-v1", ObservedAt)
    ];

    public static IReadOnlyList<CatalogProduct> Products =>
    [
        new(PartyStoreId, "P-100", "بادکنک لاتکس قرمز", "بادکنک لاتکس قرمز", "لوازم جشن/بادکنک", "بسته",
            true, "party-schema-v1", new Dictionary<string, CatalogAttributeValue>
            {
                ["material"] = new("material", CatalogValueType.String, "لاتکس", null, null, null, null, "Material", ObservedAt),
                ["diameter_cm"] = new("diameter_cm", CatalogValueType.Decimal, null, 30m, null, null, null, "DiameterCm", ObservedAt),
                ["color"] = new("color", CatalogValueType.String, "قرمز", null, null, null, null, "Color", ObservedAt)
            }),
        new(PartyStoreId, "P-101", "بادکنک فویلی ستاره‌ای", "بادکنک فویلی ستاره ای", "لوازم جشن/بادکنک", "عدد",
            true, "party-schema-v1", new Dictionary<string, CatalogAttributeValue>
            {
                ["material"] = new("material", CatalogValueType.String, "فویل", null, null, null, null, "Material", ObservedAt),
                ["shape"] = new("shape", CatalogValueType.String, "ستاره", null, null, null, null, "Shape", ObservedAt)
            }),
        new(GroceryStoreId, "G-100", "برنج ایرانی هاشمی", "برنج ایرانی هاشمی", "خواربار/برنج", "کیلوگرم",
            true, "grocery-schema-v3", new Dictionary<string, CatalogAttributeValue>
            {
                ["brand"] = new("brand", CatalogValueType.String, "محلی", null, null, null, null, "BrandName", ObservedAt),
                ["weight_kg"] = new("weight_kg", CatalogValueType.Decimal, null, 10m, null, null, null, "PackWeight", ObservedAt),
                ["organic"] = new("organic", CatalogValueType.Boolean, null, null, null, false, null, "IsOrganic", ObservedAt)
            }),
        new(GroceryStoreId, "G-101", "روغن مایع", "روغن مایع", "خواربار/روغن", "بطری",
            true, "grocery-schema-v3", new Dictionary<string, CatalogAttributeValue>
            {
                ["volume_ml"] = new("volume_ml", CatalogValueType.Integer, null, null, 1800, null, null, "VolumeMl", ObservedAt)
            }),
        new(ClothingStoreId, "C-100", "پیراهن مردانه", "پیراهن مردانه", "پوشاک/مردانه", "عدد",
            true, "clothing-schema-v2", new Dictionary<string, CatalogAttributeValue>
            {
                ["size"] = new("size", CatalogValueType.String, "L", null, null, null, null, "SizeCode", ObservedAt),
                ["fabric"] = new("fabric", CatalogValueType.String, "نخ پنبه", null, null, null, null, "FabricName", ObservedAt),
                ["fit"] = new("fit", CatalogValueType.String, "regular", null, null, null, null, "FitType", ObservedAt)
            }),
        new(ClothingStoreId, "C-101", "شلوار جین", "شلوار جین", "پوشاک/شلوار", "عدد",
            true, "clothing-schema-v2", new Dictionary<string, CatalogAttributeValue>
            {
                ["waist_size"] = new("waist_size", CatalogValueType.Integer, null, null, 32, null, null, "WaistSize", ObservedAt),
                ["fabric"] = new("fabric", CatalogValueType.String, "دنیم", null, null, null, null, "FabricName", ObservedAt)
            })
    ];


    public static IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, ProductAttributeDefinition>> AttributeDefinitions =>
        new Dictionary<Guid, IReadOnlyDictionary<string, ProductAttributeDefinition>>
        {
            [PartyStoreId] = new Dictionary<string, ProductAttributeDefinition>
            {
                ["material"] = new("material", "جنس", CatalogValueType.String, CatalogSensitivity.Public, ["لاتکس", "فویل"], true),
                ["diameter_cm"] = new("diameter_cm", "قطر (سانتی‌متر)", CatalogValueType.Decimal, CatalogSensitivity.Public, [], true),
                ["color"] = new("color", "رنگ", CatalogValueType.String, CatalogSensitivity.Public, ["قرمز", "آبی"], true),
                ["shape"] = new("shape", "شکل", CatalogValueType.String, CatalogSensitivity.Public, ["ستاره"], true)
            },
            [GroceryStoreId] = new Dictionary<string, ProductAttributeDefinition>
            {
                ["brand"] = new("brand", "برند", CatalogValueType.String, CatalogSensitivity.Public, ["محلی"], true),
                ["weight_kg"] = new("weight_kg", "وزن (کیلوگرم)", CatalogValueType.Decimal, CatalogSensitivity.Public, [], true),
                ["organic"] = new("organic", "ارگانیک", CatalogValueType.Boolean, CatalogSensitivity.Public, [], true),
                ["volume_ml"] = new("volume_ml", "حجم (میلی‌لیتر)", CatalogValueType.Integer, CatalogSensitivity.Public, [], true)
            },
            [ClothingStoreId] = new Dictionary<string, ProductAttributeDefinition>
            {
                ["size"] = new("size", "سایز", CatalogValueType.String, CatalogSensitivity.Public, ["M", "L", "XL"], true),
                ["fabric"] = new("fabric", "جنس پارچه", CatalogValueType.String, CatalogSensitivity.Public, ["نخ پنبه", "دنیم"], true),
                ["fit"] = new("fit", "قالب", CatalogValueType.String, CatalogSensitivity.Public, ["regular", "slim"], true),
                ["waist_size"] = new("waist_size", "سایز کمر", CatalogValueType.Integer, CatalogSensitivity.Public, [], true)
            }
        };

    public static IReadOnlyList<ProductAlias> Aliases =>
    [
        new(PartyStoreId, "P-100", "بادکنک قرمز", "fa-IR", "fixture:operator-approved", 0.99m, CatalogAliasStatus.Approved),
        new(PartyStoreId, "P-101", "بادکنک ستاره", "fa-IR", "fixture:operator-approved", 0.98m, CatalogAliasStatus.Approved),
        new(PartyStoreId, "P-100", "بادکنک سرخ", "fa-IR", "fixture:unreviewed-suggestion", 0.72m, CatalogAliasStatus.Proposed),
        new(GroceryStoreId, "G-100", "برنج هاشمی", "fa-IR", "fixture:operator-approved", 0.99m, CatalogAliasStatus.Approved),
        new(ClothingStoreId, "C-100", "پیراهن", "fa-IR", "fixture:operator-approved", 0.97m, CatalogAliasStatus.Approved)
    ];
}
