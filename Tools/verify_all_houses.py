from pathlib import Path

# Verify that all 22 cities and their flags exist
COUNTRIES = [
    ("India", ["Delhi", "Bengaluru", "Mumbai"]),
    ("Brazil", ["Brasilia", "Rio de Janeiro", "Sao Paulo"]),
    ("China", ["Beijing", "Shenzhen", "Shanghai"]),
    ("Russia", ["Kazan", "Saint Petersburg", "Moscow"]),
    ("Italy", ["Rome", "Milan"]),
    ("France", ["Marseille", "Paris"]),
    ("UnitedKingdom", ["Liverpool", "Manchester", "London"]),
    ("UnitedStates", ["San Francisco", "Los Angeles", "New York"]),
]

RES_DIR = Path("Assets/_Ropoly/Resources/CityTiles")

print("Checking Flag Assets:")
all_ok = True
for country, cities in COUNTRIES:
    base = RES_DIR / f"{country}FlagPanel.png"
    soft = RES_DIR / f"{country}FlagPanel_Soft.png"
    blur = RES_DIR / f"{country}FlagPanel_Blur.png"
    
    base_meta = RES_DIR / f"{country}FlagPanel.png.meta"
    soft_meta = RES_DIR / f"{country}FlagPanel_Soft.png.meta"
    blur_meta = RES_DIR / f"{country}FlagPanel_Blur.png.meta"

    exists = all(p.exists() for p in [base, soft, blur, base_meta, soft_meta, blur_meta])
    status = "OK" if exists else "MISSING"
    if not exists:
        all_ok = False
    print(f"  [{status}] {country} ({len(cities)} cities): {base.name}, {soft.name}, {blur.name}")

total_cities = sum(len(cities) for _, cities in COUNTRIES)
print(f"\nTotal city properties: {total_cities} across {len(COUNTRIES)} countries.")
print(f"All flag assets verified: {all_ok}")
