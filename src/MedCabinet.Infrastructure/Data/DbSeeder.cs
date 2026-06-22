using MedCabinet.Domain.Entities;
using MedCabinet.Domain.Enums;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace MedCabinet.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Users.AnyAsync())
        {
            return;
        }

        // 创建用户
        var users = new List<User>
        {
            new User
            {
                Username = "zhangsan",
                Email = "zhangsan@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Avatar = "https://api.dicebear.com/7.x/avataaars/svg?seed=zhangsan",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new User
            {
                Username = "lisi",
                Email = "lisi@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Avatar = "https://api.dicebear.com/7.x/avataaars/svg?seed=lisi",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new User
            {
                Username = "wangwu",
                Email = "wangwu@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Avatar = "https://api.dicebear.com/7.x/avataaars/svg?seed=wangwu",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new User
            {
                Username = "zhaoliu",
                Email = "zhaoliu@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Avatar = "https://api.dicebear.com/7.x/avataaars/svg?seed=zhaoliu",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            },
            new User
            {
                Username = "qianqi",
                Email = "qianqi@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                Avatar = "https://api.dicebear.com/7.x/avataaars/svg?seed=qianqi",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        // 创建家庭
        var households = new List<Household>
        {
            new Household
            {
                Name = "张三的家庭药柜",
                InviteCode = "FAM123456",
                CreatedBy = users[0].Id,
                CreatedAt = DateTime.Now
            },
            new Household
            {
                Name = "李四的家庭药柜",
                InviteCode = "FAM654321",
                CreatedBy = users[1].Id,
                CreatedAt = DateTime.Now
            }
        };

        await context.Households.AddRangeAsync(households);
        await context.SaveChangesAsync();

        // 创建家庭成员
        var householdMembers = new List<HouseholdMember>
        {
            new HouseholdMember
            {
                HouseholdId = households[0].Id,
                UserId = users[0].Id,
                Role = "Owner",
                JoinedAt = DateTime.Now
            },
            new HouseholdMember
            {
                HouseholdId = households[0].Id,
                UserId = users[2].Id,
                Role = "Member",
                JoinedAt = DateTime.Now
            },
            new HouseholdMember
            {
                HouseholdId = households[0].Id,
                UserId = users[3].Id,
                Role = "Admin",
                JoinedAt = DateTime.Now
            },
            new HouseholdMember
            {
                HouseholdId = households[1].Id,
                UserId = users[1].Id,
                Role = "Owner",
                JoinedAt = DateTime.Now
            },
            new HouseholdMember
            {
                HouseholdId = households[1].Id,
                UserId = users[4].Id,
                Role = "Member",
                JoinedAt = DateTime.Now
            }
        };

        await context.HouseholdMembers.AddRangeAsync(householdMembers);
        await context.SaveChangesAsync();

        // 创建药品
        var medicines = new List<Medicine>
        {
            new Medicine
            {
                HouseholdId = households[0].Id,
                Name = "布洛芬缓释胶囊",
                Category = "解热镇痛",
                Indication = "用于缓解轻至中度疼痛，如头痛、关节痛、偏头痛、牙痛、肌肉痛、神经痛、痛经。也用于普通感冒或流行性感冒引起的发热。",
                Dosage = "口服。成人，一次1粒，一日2次（早晚各一次）。",
                ExpiryDate = DateTime.Now.AddMonths(6),
                StockQuantity = 24,
                StorageLocation = "客厅药箱第一层",
                Contraindications = "对其他非甾体抗炎药过敏者禁用。孕妇及哺乳期妇女禁用。对阿司匹林过敏的哮喘患者禁用。",
                PhotoUrl = "https://example.com/ibuprofen.jpg",
                Status = MedicineStatus.Valid,
                CreatedAt = DateTime.Now
            },
            new Medicine
            {
                HouseholdId = households[0].Id,
                Name = "感冒灵颗粒",
                Category = "感冒用药",
                Indication = "解热镇痛。用于感冒引起的头痛，发热，鼻塞，流涕，咽痛。",
                Dosage = "开水冲服，一次1袋，一日3次。",
                ExpiryDate = DateTime.Now.AddMonths(12),
                StockQuantity = 10,
                StorageLocation = "客厅药箱第二层",
                Contraindications = "严重肝肾功能不全者禁用。",
                PhotoUrl = "https://example.com/ganmaoling.jpg",
                Status = MedicineStatus.Valid,
                CreatedAt = DateTime.Now
            },
            new Medicine
            {
                HouseholdId = households[0].Id,
                Name = "阿莫西林胶囊",
                Category = "抗生素",
                Indication = "用于敏感菌（不产β内酰胺酶菌株）所致的下列感染：中耳炎、鼻窦炎、咽炎、扁桃体炎等上呼吸道感染。",
                Dosage = "口服。成人一次0.5g，每6～8小时1次，一日剂量不超过4g。",
                ExpiryDate = DateTime.Now.AddDays(20),
                StockQuantity = 30,
                StorageLocation = "卧室床头柜",
                Contraindications = "青霉素过敏及青霉素皮肤试验阳性患者禁用。",
                PhotoUrl = "https://example.com/amoxicillin.jpg",
                Status = MedicineStatus.NearExpiry,
                CreatedAt = DateTime.Now
            },
            new Medicine
            {
                HouseholdId = households[0].Id,
                Name = "创可贴",
                Category = "外伤用药",
                Indication = "用于小创伤、擦伤等患处。",
                Dosage = "外用。撕去覆盖薄膜，将中间复合垫贴在创伤处，两端橡皮膏固定。",
                ExpiryDate = DateTime.Now.AddYears(2),
                StockQuantity = 50,
                StorageLocation = "客厅药箱第三层",
                Contraindications = "对本品过敏者禁用，过敏体质者慎用。",
                PhotoUrl = "https://example.com/bandaid.jpg",
                Status = MedicineStatus.Valid,
                CreatedAt = DateTime.Now
            },
            new Medicine
            {
                HouseholdId = households[0].Id,
                Name = "维生素C片",
                Category = "维生素补充",
                Indication = "用于预防坏血病，也可用于各种急慢性传染疾病及紫癜等的辅助治疗。",
                Dosage = "口服。用于补充维生素C：成人一日1片。",
                ExpiryDate = DateTime.Now.AddYears(1),
                StockQuantity = 100,
                StorageLocation = "客厅药箱第二层",
                Contraindications = "不宜长期过量服用本品，否则，突然停药有可能出现坏血病症状。",
                PhotoUrl = "https://example.com/vitaminc.jpg",
                Status = MedicineStatus.Valid,
                CreatedAt = DateTime.Now
            },
            new Medicine
            {
                HouseholdId = households[0].Id,
                Name = "过期感冒药",
                Category = "感冒用药",
                Indication = "用于感冒引起的头痛、发热。",
                Dosage = "口服，一次1片，一日3次。",
                ExpiryDate = DateTime.Now.AddDays(-30),
                StockQuantity = 5,
                StorageLocation = "储物间旧药箱",
                Contraindications = "严重肝肾功能不全者禁用。",
                PhotoUrl = "https://example.com/expired.jpg",
                Status = MedicineStatus.Expired,
                CreatedAt = DateTime.Now
            },
            new Medicine
            {
                HouseholdId = households[0].Id,
                Name = "健胃消食片",
                Category = "消化用药",
                Indication = "健胃消食。用于脾胃虚弱所致的食积，症见不思饮食、嗳腐酸臭、脘腹胀满；消化不良见上述证候者。",
                Dosage = "口服，可以咀嚼。一次4-6片，一日3次。",
                ExpiryDate = DateTime.Now.AddMonths(18),
                StockQuantity = 36,
                StorageLocation = "客厅药箱第一层",
                Contraindications = "尚不明确。",
                PhotoUrl = "https://example.com/jianwei.jpg",
                Status = MedicineStatus.Valid,
                CreatedAt = DateTime.Now
            },
            new Medicine
            {
                HouseholdId = households[1].Id,
                Name = "复方甘草片",
                Category = "镇咳祛痰",
                Indication = "用于镇咳祛痰。",
                Dosage = "口服或含化。成人一次3-4片，一日3次。",
                ExpiryDate = DateTime.Now.AddMonths(8),
                StockQuantity = 50,
                StorageLocation = "主卧床头柜",
                Contraindications = "对本品成分过敏者禁用。",
                PhotoUrl = "https://example.com/liquorice.jpg",
                Status = MedicineStatus.Valid,
                CreatedAt = DateTime.Now
            },
            new Medicine
            {
                HouseholdId = households[1].Id,
                Name = "碘伏消毒液",
                Category = "消毒用品",
                Indication = "用于皮肤、黏膜的消毒。",
                Dosage = "外用。用棉签蘸取少量，由中心向外周局部涂搽。",
                ExpiryDate = DateTime.Now.AddMonths(3),
                StockQuantity = 2,
                StorageLocation = "卫生间药柜",
                Contraindications = "对碘过敏者禁用。",
                PhotoUrl = "https://example.com/iodine.jpg",
                Status = MedicineStatus.NearExpiry,
                CreatedAt = DateTime.Now
            },
            new Medicine
            {
                HouseholdId = households[1].Id,
                Name = "降压药（示例）",
                Category = "心血管用药",
                Indication = "用于高血压的治疗。",
                Dosage = "口服，一次1片，一日1次。",
                ExpiryDate = DateTime.Now.AddYears(1),
                StockQuantity = 28,
                StorageLocation = "老人卧室床头柜",
                Contraindications = "对本品过敏者禁用。",
                PhotoUrl = "https://example.com/bpmedicine.jpg",
                Status = MedicineStatus.Valid,
                CreatedAt = DateTime.Now
            }
        };

        await context.Medicines.AddRangeAsync(medicines);
        await context.SaveChangesAsync();

        // 创建用药记录
        var medUsages = new List<MedUsage>
        {
            new MedUsage
            {
                MedicineId = medicines[0].Id,
                UserId = users[0].Id,
                UsedBy = "张三",
                UsedQuantity = 1,
                UsedAt = DateTime.Now.AddDays(-2),
                SymptomNote = "头痛"
            },
            new MedUsage
            {
                MedicineId = medicines[1].Id,
                UserId = users[2].Id,
                UsedBy = "王五",
                UsedQuantity = 1,
                UsedAt = DateTime.Now.AddDays(-1),
                SymptomNote = "感冒发烧"
            },
            new MedUsage
            {
                MedicineId = medicines[3].Id,
                UserId = users[0].Id,
                UsedBy = "张三",
                UsedQuantity = 2,
                UsedAt = DateTime.Now.AddDays(-5),
                SymptomNote = "手指擦伤"
            },
            new MedUsage
            {
                MedicineId = medicines[6].Id,
                UserId = users[3].Id,
                UsedBy = "赵六",
                UsedQuantity = 4,
                UsedAt = DateTime.Now.AddHours(-12),
                SymptomNote = "消化不良"
            },
            new MedUsage
            {
                MedicineId = medicines[8].Id,
                UserId = users[1].Id,
                UsedBy = "李四",
                UsedQuantity = 1,
                UsedAt = DateTime.Now.AddDays(-3),
                SymptomNote = "伤口消毒"
            },
            new MedUsage
            {
                MedicineId = medicines[9].Id,
                UserId = users[4].Id,
                UsedBy = "钱七（父）",
                UsedQuantity = 1,
                UsedAt = DateTime.Now.AddHours(-8),
                SymptomNote = "每日降压"
            }
        };

        await context.MedUsages.AddRangeAsync(medUsages);
        await context.SaveChangesAsync();

        // 创建提醒
        var medAlerts = new List<MedAlert>
        {
            new MedAlert
            {
                MedicineId = medicines[2].Id,
                UserId = users[0].Id,
                AlertType = AlertType.NearExpiry,
                Message = "阿莫西林胶囊将在20天后过期，请及时处理。",
                IsRead = false,
                CreatedAt = DateTime.Now
            },
            new MedAlert
            {
                MedicineId = medicines[5].Id,
                UserId = users[0].Id,
                AlertType = AlertType.Expired,
                Message = "过期感冒药已过期30天，请立即清理。",
                IsRead = false,
                CreatedAt = DateTime.Now
            },
            new MedAlert
            {
                MedicineId = medicines[8].Id,
                UserId = users[1].Id,
                AlertType = AlertType.NearExpiry,
                Message = "碘伏消毒液将在3个月后过期，请检查。",
                IsRead = false,
                CreatedAt = DateTime.Now
            }
        };

        await context.MedAlerts.AddRangeAsync(medAlerts);
        await context.SaveChangesAsync();
    }
}
