namespace RentalManager.Api.Common;

/// <summary>使用者輸入或狀態不符合規則，對應 HTTP 400。</summary>
public class DomainValidationException(string message) : Exception(message);

/// <summary>資源不存在，對應 HTTP 404。</summary>
public class DomainNotFoundException(string? message = null) : Exception(message ?? string.Empty);

/// <summary>驗證失敗，對應 HTTP 401。</summary>
public class DomainUnauthorizedException(string? message = null) : Exception(message ?? string.Empty);
