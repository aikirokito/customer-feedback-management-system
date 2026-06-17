-- ==================================================================================
-- SQL script to seed test accounts for Customer Feedback Management System (CFMS)
-- For development environment only.
--
-- Accounts details:
-- 1. customer1@test.com  -> Role: Customer
-- 2. staff1@test.com     -> Role: SupportStaff
-- 3. manager1@test.com   -> Role: DepartmentManager
-- 4. admin1@test.com     -> Role: SystemAdmin
--
-- ALL accounts use password: Password123!
-- Verified BCrypt Hash (workFactor: 12): $2a$12$8E6RWISTi5Rgm3Ys/pK.Wu0bvRmOcQyJ4x7WfE0cGPoqFIpYrCl/m
-- ==================================================================================

INSERT INTO users (
    "Id", "Email", "PasswordHash", "FirstName", "LastName", "PhoneNumber", "AvatarUrl", 
    "Role", "Status", "IsEmailVerified", "GoogleSubject", "LastLoginAtUtc", 
    "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted"
) VALUES 
-- Customer Account
(
    'a3b3c3d3-0000-0000-0000-000000000001', 
    'customer1@test.com', 
    '$2a$12$8E6RWISTi5Rgm3Ys/pK.Wu0bvRmOcQyJ4x7WfE0cGPoqFIpYrCl/m', 
    'Khách', 
    'Hàng 1', 
    '0901234567', 
    NULL, 
    'Customer', 
    'Active', 
    true, 
    NULL, 
    NULL, 
    NOW() AT TIME ZONE 'UTC', 
    NOW() AT TIME ZONE 'UTC', 
    false
),
-- Support Staff Account
(
    'a3b3c3d3-0000-0000-0000-000000000002', 
    'staff1@test.com', 
    '$2a$12$8E6RWISTi5Rgm3Ys/pK.Wu0bvRmOcQyJ4x7WfE0cGPoqFIpYrCl/m', 
    'Nhân', 
    'Viên 1', 
    '0902345678', 
    NULL, 
    'SupportStaff', 
    'Active', 
    true, 
    NULL, 
    NULL, 
    NOW() AT TIME ZONE 'UTC', 
    NOW() AT TIME ZONE 'UTC', 
    false
),
-- Department Manager Account
(
    'a3b3c3d3-0000-0000-0000-000000000003', 
    'manager1@test.com', 
    '$2a$12$8E6RWISTi5Rgm3Ys/pK.Wu0bvRmOcQyJ4x7WfE0cGPoqFIpYrCl/m', 
    'Trưởng', 
    'Phòng 1', 
    '0903456789', 
    NULL, 
    'DepartmentManager', 
    'Active', 
    true, 
    NULL, 
    NULL, 
    NOW() AT TIME ZONE 'UTC', 
    NOW() AT TIME ZONE 'UTC', 
    false
),
-- System Admin Account
(
    'a3b3c3d3-0000-0000-0000-000000000004', 
    'admin1@test.com', 
    '$2a$12$8E6RWISTi5Rgm3Ys/pK.Wu0bvRmOcQyJ4x7WfE0cGPoqFIpYrCl/m', 
    'Quản', 
    'Trị 1', 
    '0904567890', 
    NULL, 
    'SystemAdmin', 
    'Active', 
    true, 
    NULL, 
    NULL, 
    NOW() AT TIME ZONE 'UTC', 
    NOW() AT TIME ZONE 'UTC', 
    false
)
ON CONFLICT ("Email") DO UPDATE SET
    "PasswordHash" = EXCLUDED."PasswordHash",
    "FirstName" = EXCLUDED."FirstName",
    "LastName" = EXCLUDED."LastName",
    "Role" = EXCLUDED."Role",
    "Status" = EXCLUDED."Status";
