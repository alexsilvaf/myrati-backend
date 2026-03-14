using FluentValidation.TestHelper;
using Myrati.Application.Contracts;
using Myrati.Application.Validation;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class ValidatorTests
{
    // ── Auth ─────────────────────────────────────────────────────────────

    [Fact]
    public void LoginValidator_ValidCredentials_Passes()
    {
        var validator = new LoginRequestValidator();
        var result = validator.TestValidate(new LoginRequest("user@example.com", "secret123"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LoginValidator_EmptyEmail_Fails()
    {
        var validator = new LoginRequestValidator();
        var result = validator.TestValidate(new LoginRequest("", "secret123"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void LoginValidator_InvalidEmail_Fails()
    {
        var validator = new LoginRequestValidator();
        var result = validator.TestValidate(new LoginRequest("not-an-email", "secret123"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void LoginValidator_EmptyPassword_Fails()
    {
        var validator = new LoginRequestValidator();
        var result = validator.TestValidate(new LoginRequest("user@example.com", ""));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void LoginValidator_ShortPassword_Fails()
    {
        var validator = new LoginRequestValidator();
        var result = validator.TestValidate(new LoginRequest("user@example.com", "abc"));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void PasswordSetupValidator_ValidData_Passes()
    {
        var validator = new PasswordSetupRequestValidator();
        var result = validator.TestValidate(new PasswordSetupRequest("tok123", "StrongPass1", "StrongPass1"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PasswordSetupValidator_EmptyToken_Fails()
    {
        var validator = new PasswordSetupRequestValidator();
        var result = validator.TestValidate(new PasswordSetupRequest("", "StrongPass1", "StrongPass1"));
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void PasswordSetupValidator_ShortPassword_Fails()
    {
        var validator = new PasswordSetupRequestValidator();
        var result = validator.TestValidate(new PasswordSetupRequest("tok123", "short", "short"));
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void PasswordSetupValidator_MismatchedPasswords_Fails()
    {
        var validator = new PasswordSetupRequestValidator();
        var result = validator.TestValidate(new PasswordSetupRequest("tok123", "StrongPass1", "Different1"));
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    // ── Clients ──────────────────────────────────────────────────────────

    [Fact]
    public void CreateClientValidator_ValidData_Passes()
    {
        var validator = new CreateClientRequestValidator();
        var result = validator.TestValidate(new CreateClientRequest(
            "Acme Corp", "acme@example.com", "+5511999990000", "12345678901", "CPF", "Acme Corp Ltda", "Ativo"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateClientValidator_EmptyName_Fails()
    {
        var validator = new CreateClientRequestValidator();
        var result = validator.TestValidate(new CreateClientRequest(
            "", "acme@example.com", "+5511999990000", "12345678901", "CPF", "Acme Corp Ltda", "Ativo"));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateClientValidator_InvalidEmail_Fails()
    {
        var validator = new CreateClientRequestValidator();
        var result = validator.TestValidate(new CreateClientRequest(
            "Acme Corp", "bad-email", "+5511999990000", "12345678901", "CPF", "Acme Corp Ltda", "Ativo"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void CreateClientValidator_InvalidDocumentType_Fails()
    {
        var validator = new CreateClientRequestValidator();
        var result = validator.TestValidate(new CreateClientRequest(
            "Acme Corp", "acme@example.com", "+5511999990000", "12345678901", "RG", "Acme Corp Ltda", "Ativo"));
        result.ShouldHaveValidationErrorFor(x => x.DocumentType);
    }

    [Fact]
    public void CreateClientValidator_InvalidStatus_Fails()
    {
        var validator = new CreateClientRequestValidator();
        var result = validator.TestValidate(new CreateClientRequest(
            "Acme Corp", "acme@example.com", "+5511999990000", "12345678901", "CPF", "Acme Corp Ltda", "Deleted"));
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void UpdateClientValidator_ValidData_Passes()
    {
        var validator = new UpdateClientRequestValidator();
        var result = validator.TestValidate(new UpdateClientRequest(
            "Acme Corp", "acme@example.com", "+5511999990000", "12345678901234", "CNPJ", "Acme Corp Ltda", "Inativo"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateClientValidator_EmptyPhone_Fails()
    {
        var validator = new UpdateClientRequestValidator();
        var result = validator.TestValidate(new UpdateClientRequest(
            "Acme Corp", "acme@example.com", "", "12345678901", "CPF", "Acme Corp Ltda", "Ativo"));
        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void UpdateClientValidator_EmptyCompany_Fails()
    {
        var validator = new UpdateClientRequestValidator();
        var result = validator.TestValidate(new UpdateClientRequest(
            "Acme Corp", "acme@example.com", "+5511999990000", "12345678901", "CPF", "", "Ativo"));
        result.ShouldHaveValidationErrorFor(x => x.Company);
    }

    // ── Company Costs ────────────────────────────────────────────────────

    [Fact]
    public void CreateCompanyCostValidator_ValidData_Passes()
    {
        var validator = new CreateCompanyCostRequestValidator();
        var result = validator.TestValidate(new CreateCompanyCostRequest(
            "AWS", "Cloud hosting", "cloud", 1500.00m, "monthly", "Amazon", "2025-01-01", null, "Ativo"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateCompanyCostValidator_EmptyName_Fails()
    {
        var validator = new CreateCompanyCostRequestValidator();
        var result = validator.TestValidate(new CreateCompanyCostRequest(
            "", "Cloud hosting", "cloud", 1500.00m, "monthly", "Amazon", "2025-01-01", null, "Ativo"));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateCompanyCostValidator_InvalidCategory_Fails()
    {
        var validator = new CreateCompanyCostRequestValidator();
        var result = validator.TestValidate(new CreateCompanyCostRequest(
            "AWS", "Cloud hosting", "invalid_cat", 1500.00m, "monthly", "Amazon", "2025-01-01", null, "Ativo"));
        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void CreateCompanyCostValidator_ZeroAmount_Fails()
    {
        var validator = new CreateCompanyCostRequestValidator();
        var result = validator.TestValidate(new CreateCompanyCostRequest(
            "AWS", "Cloud hosting", "cloud", 0m, "monthly", "Amazon", "2025-01-01", null, "Ativo"));
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void CreateCompanyCostValidator_InvalidRecurrence_Fails()
    {
        var validator = new CreateCompanyCostRequestValidator();
        var result = validator.TestValidate(new CreateCompanyCostRequest(
            "AWS", "Cloud hosting", "cloud", 1500.00m, "weekly", "Amazon", "2025-01-01", null, "Ativo"));
        result.ShouldHaveValidationErrorFor(x => x.Recurrence);
    }

    [Fact]
    public void CreateCompanyCostValidator_InvalidStatus_Fails()
    {
        var validator = new CreateCompanyCostRequestValidator();
        var result = validator.TestValidate(new CreateCompanyCostRequest(
            "AWS", "Cloud hosting", "cloud", 1500.00m, "monthly", "Amazon", "2025-01-01", null, "Deleted"));
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void CreateCompanyCostValidator_EmptyVendor_Fails()
    {
        var validator = new CreateCompanyCostRequestValidator();
        var result = validator.TestValidate(new CreateCompanyCostRequest(
            "AWS", "Cloud hosting", "cloud", 1500.00m, "monthly", "", "2025-01-01", null, "Ativo"));
        result.ShouldHaveValidationErrorFor(x => x.Vendor);
    }

    [Fact]
    public void CreateCompanyCostValidator_EmptyStartDate_Fails()
    {
        var validator = new CreateCompanyCostRequestValidator();
        var result = validator.TestValidate(new CreateCompanyCostRequest(
            "AWS", "Cloud hosting", "cloud", 1500.00m, "monthly", "Amazon", "", null, "Ativo"));
        result.ShouldHaveValidationErrorFor(x => x.StartDate);
    }

    // ── Products: License ────────────────────────────────────────────────

    [Fact]
    public void CreateLicenseValidator_ValidData_Passes()
    {
        var validator = new CreateLicenseRequestValidator();
        var result = validator.TestValidate(new CreateLicenseRequest(
            "client-001", "Professional", 299.90m, null, null, "2025-01-01", "2026-01-01"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateLicenseValidator_EmptyClientId_Fails()
    {
        var validator = new CreateLicenseRequestValidator();
        var result = validator.TestValidate(new CreateLicenseRequest(
            "", "Professional", 299.90m, null, null, "2025-01-01", "2026-01-01"));
        result.ShouldHaveValidationErrorFor(x => x.ClientId);
    }

    [Fact]
    public void CreateLicenseValidator_EmptyPlan_Fails()
    {
        var validator = new CreateLicenseRequestValidator();
        var result = validator.TestValidate(new CreateLicenseRequest(
            "client-001", "", 299.90m, null, null, "2025-01-01", "2026-01-01"));
        result.ShouldHaveValidationErrorFor(x => x.Plan);
    }

    [Fact]
    public void CreateLicenseValidator_ZeroMonthlyValue_Fails()
    {
        var validator = new CreateLicenseRequestValidator();
        var result = validator.TestValidate(new CreateLicenseRequest(
            "client-001", "Professional", 0m, null, null, "2025-01-01", "2026-01-01"));
        result.ShouldHaveValidationErrorFor(x => x.MonthlyValue);
    }

    [Fact]
    public void CreateLicenseValidator_EmptyStartDate_Fails()
    {
        var validator = new CreateLicenseRequestValidator();
        var result = validator.TestValidate(new CreateLicenseRequest(
            "client-001", "Professional", 299.90m, null, null, "", "2026-01-01"));
        result.ShouldHaveValidationErrorFor(x => x.StartDate);
    }

    [Fact]
    public void CreateLicenseValidator_EmptyExpiryDate_Fails()
    {
        var validator = new CreateLicenseRequestValidator();
        var result = validator.TestValidate(new CreateLicenseRequest(
            "client-001", "Professional", 299.90m, null, null, "2025-01-01", ""));
        result.ShouldHaveValidationErrorFor(x => x.ExpiryDate);
    }

    // ── Products: Plan ───────────────────────────────────────────────────

    [Fact]
    public void UpsertProductPlanValidator_ValidData_Passes()
    {
        var validator = new UpsertProductPlanRequestValidator();
        var result = validator.TestValidate(new UpsertProductPlanRequest(
            "Enterprise", 50, 499.90m, null, null, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpsertProductPlanValidator_NullMaxUsers_Passes()
    {
        var validator = new UpsertProductPlanRequestValidator();
        var result = validator.TestValidate(new UpsertProductPlanRequest(
            "Unlimited", null, 999.90m, null, null, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpsertProductPlanValidator_EmptyName_Fails()
    {
        var validator = new UpsertProductPlanRequestValidator();
        var result = validator.TestValidate(new UpsertProductPlanRequest(
            "", 10, 100m, null, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void UpsertProductPlanValidator_ZeroMaxUsers_Fails()
    {
        var validator = new UpsertProductPlanRequestValidator();
        var result = validator.TestValidate(new UpsertProductPlanRequest(
            "Basic", 0, 100m, null, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.MaxUsers);
    }

    [Fact]
    public void UpsertProductPlanValidator_NegativeMonthlyPrice_Fails()
    {
        var validator = new UpsertProductPlanRequestValidator();
        var result = validator.TestValidate(new UpsertProductPlanRequest(
            "Basic", 10, -1m, null, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.MonthlyPrice);
    }

    [Fact]
    public void UpsertProductPlanValidator_ZeroMonthlyPrice_Passes()
    {
        var validator = new UpsertProductPlanRequestValidator();
        var result = validator.TestValidate(new UpsertProductPlanRequest(
            "Free", null, 0m, null, null, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ── Profile ──────────────────────────────────────────────────────────

    [Fact]
    public void UpdateProfileValidator_ValidData_Passes()
    {
        var validator = new UpdateProfileRequestValidator();
        var result = validator.TestValidate(new UpdateProfileRequest(
            "John Doe", "john@example.com", "+5511999990000", "Engineering", "Sao Paulo"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateProfileValidator_EmptyName_Fails()
    {
        var validator = new UpdateProfileRequestValidator();
        var result = validator.TestValidate(new UpdateProfileRequest(
            "", "john@example.com", "+5511999990000", "Engineering", "Sao Paulo"));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void UpdateProfileValidator_InvalidEmail_Fails()
    {
        var validator = new UpdateProfileRequestValidator();
        var result = validator.TestValidate(new UpdateProfileRequest(
            "John Doe", "not-valid", "+5511999990000", "Engineering", "Sao Paulo"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void UpdateProfileValidator_EmptyPhone_Fails()
    {
        var validator = new UpdateProfileRequestValidator();
        var result = validator.TestValidate(new UpdateProfileRequest(
            "John Doe", "john@example.com", "", "Engineering", "Sao Paulo"));
        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void UpdateProfileValidator_EmptyDepartment_Fails()
    {
        var validator = new UpdateProfileRequestValidator();
        var result = validator.TestValidate(new UpdateProfileRequest(
            "John Doe", "john@example.com", "+5511999990000", "", "Sao Paulo"));
        result.ShouldHaveValidationErrorFor(x => x.Department);
    }

    [Fact]
    public void UpdateProfileValidator_EmptyLocation_Fails()
    {
        var validator = new UpdateProfileRequestValidator();
        var result = validator.TestValidate(new UpdateProfileRequest(
            "John Doe", "john@example.com", "+5511999990000", "Engineering", ""));
        result.ShouldHaveValidationErrorFor(x => x.Location);
    }

    [Fact]
    public void ChangePasswordValidator_ValidData_Passes()
    {
        var validator = new ChangePasswordRequestValidator();
        var result = validator.TestValidate(new ChangePasswordRequest("OldPass123", "NewSecure1", "NewSecure1"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ChangePasswordValidator_EmptyCurrentPassword_Fails()
    {
        var validator = new ChangePasswordRequestValidator();
        var result = validator.TestValidate(new ChangePasswordRequest("", "NewSecure1", "NewSecure1"));
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Fact]
    public void ChangePasswordValidator_ShortNewPassword_Fails()
    {
        var validator = new ChangePasswordRequestValidator();
        var result = validator.TestValidate(new ChangePasswordRequest("OldPass123", "short", "short"));
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void ChangePasswordValidator_MismatchedPasswords_Fails()
    {
        var validator = new ChangePasswordRequestValidator();
        var result = validator.TestValidate(new ChangePasswordRequest("OldPass123", "NewSecure1", "Different1"));
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    // ── Public ───────────────────────────────────────────────────────────

    [Fact]
    public void ContactValidator_ValidData_Passes()
    {
        var validator = new ContactRequestValidator();
        var result = validator.TestValidate(new ContactRequest(
            "Jane Doe", "jane@example.com", "Acme Corp", "Sales inquiry", "I would like to learn more about your platform."));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ContactValidator_EmptyName_Fails()
    {
        var validator = new ContactRequestValidator();
        var result = validator.TestValidate(new ContactRequest(
            "", "jane@example.com", "Acme Corp", "Sales inquiry", "Hello"));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ContactValidator_InvalidEmail_Fails()
    {
        var validator = new ContactRequestValidator();
        var result = validator.TestValidate(new ContactRequest(
            "Jane Doe", "bad", "Acme Corp", "Sales inquiry", "Hello"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ContactValidator_EmptyMessage_Fails()
    {
        var validator = new ContactRequestValidator();
        var result = validator.TestValidate(new ContactRequest(
            "Jane Doe", "jane@example.com", "Acme Corp", "Sales inquiry", ""));
        result.ShouldHaveValidationErrorFor(x => x.Message);
    }

    [Fact]
    public void ContactValidator_MessageExceeds2000Chars_Fails()
    {
        var validator = new ContactRequestValidator();
        var result = validator.TestValidate(new ContactRequest(
            "Jane Doe", "jane@example.com", "Acme Corp", "Sales inquiry", new string('x', 2001)));
        result.ShouldHaveValidationErrorFor(x => x.Message);
    }

    [Fact]
    public void LicenseActivationValidator_ValidData_Passes()
    {
        var validator = new LicenseActivationRequestValidator();
        var result = validator.TestValidate(new LicenseActivationRequest("prod-001", "ABCD-1234-EFGH-5678"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void LicenseActivationValidator_EmptyProductId_Fails()
    {
        var validator = new LicenseActivationRequestValidator();
        var result = validator.TestValidate(new LicenseActivationRequest("", "ABCD-1234-EFGH-5678"));
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void LicenseActivationValidator_EmptyLicenseKey_Fails()
    {
        var validator = new LicenseActivationRequestValidator();
        var result = validator.TestValidate(new LicenseActivationRequest("prod-001", ""));
        result.ShouldHaveValidationErrorFor(x => x.LicenseKey);
    }

    // ── Settings ─────────────────────────────────────────────────────────

    [Fact]
    public void CreateApiKeyValidator_ValidData_Passes()
    {
        var validator = new CreateApiKeyRequestValidator();
        var result = validator.TestValidate(new CreateApiKeyRequest("My API Key", "production"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateApiKeyValidator_StagingEnvironment_Passes()
    {
        var validator = new CreateApiKeyRequestValidator();
        var result = validator.TestValidate(new CreateApiKeyRequest("Staging Key", "staging"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateApiKeyValidator_EmptyLabel_Fails()
    {
        var validator = new CreateApiKeyRequestValidator();
        var result = validator.TestValidate(new CreateApiKeyRequest("", "production"));
        result.ShouldHaveValidationErrorFor(x => x.Label);
    }

    [Fact]
    public void CreateApiKeyValidator_LabelExceeds80Chars_Fails()
    {
        var validator = new CreateApiKeyRequestValidator();
        var result = validator.TestValidate(new CreateApiKeyRequest(new string('a', 81), "production"));
        result.ShouldHaveValidationErrorFor(x => x.Label);
    }

    [Fact]
    public void CreateApiKeyValidator_InvalidEnvironment_Fails()
    {
        var validator = new CreateApiKeyRequestValidator();
        var result = validator.TestValidate(new CreateApiKeyRequest("My Key", "development"));
        result.ShouldHaveValidationErrorFor(x => x.Environment);
    }

    [Fact]
    public void CreateTeamMemberValidator_ValidData_Passes()
    {
        var validator = new CreateTeamMemberRequestValidator();
        var result = validator.TestValidate(new CreateTeamMemberRequest("Maria Silva", "maria@example.com", "Admin"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateTeamMemberValidator_VendedorRole_Passes()
    {
        var validator = new CreateTeamMemberRequestValidator();
        var result = validator.TestValidate(new CreateTeamMemberRequest("Carlos", "carlos@example.com", "Vendedor"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateTeamMemberValidator_DesenvolvedorRole_Passes()
    {
        var validator = new CreateTeamMemberRequestValidator();
        var result = validator.TestValidate(new CreateTeamMemberRequest("Ana", "ana@example.com", "Desenvolvedor"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateTeamMemberValidator_EmptyName_Fails()
    {
        var validator = new CreateTeamMemberRequestValidator();
        var result = validator.TestValidate(new CreateTeamMemberRequest("", "maria@example.com", "Admin"));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateTeamMemberValidator_InvalidEmail_Fails()
    {
        var validator = new CreateTeamMemberRequestValidator();
        var result = validator.TestValidate(new CreateTeamMemberRequest("Maria Silva", "not-email", "Admin"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void CreateTeamMemberValidator_InvalidRole_Fails()
    {
        var validator = new CreateTeamMemberRequestValidator();
        var result = validator.TestValidate(new CreateTeamMemberRequest("Maria Silva", "maria@example.com", "Manager"));
        result.ShouldHaveValidationErrorFor(x => x.Role);
    }
}
