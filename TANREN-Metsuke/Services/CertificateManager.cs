using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace TANREN_Metsuke.Services;

public static class CertificateManager
{
    private const string CertSubject = "CN=TANREN Metsuke Sync";
    private const string ServerAuthOid = "1.3.6.1.5.5.7.3.1";
    private static readonly string CertPath = Path.Combine(AppPaths.BaseDir, "sync_cert.pfx");

    // Exportable flag makes the key non-ephemeral so Windows can use it
    private const X509KeyStorageFlags LoadFlags = X509KeyStorageFlags.Exportable;

    public static X509Certificate2 LoadOrCreate()
    {
        if (File.Exists(CertPath))
        {
            try
            {
                var cert = X509CertificateLoader.LoadPkcs12FromFile(CertPath, password: null, LoadFlags);
                if (cert.NotAfter > DateTime.UtcNow && cert.HasPrivateKey)
                    return cert;
                cert.Dispose();
            }
            catch
            {
                // Corrupt, we must regenerate
            }
        }
        return Generate();
    }

    public static string Fingerprint(X509Certificate2 cert) => Convert.ToHexString(SHA256.HashData(cert.RawData)).ToLowerInvariant();

    private static X509Certificate2 Generate()
    {
        using var rsa = RSA.Create(2048);
        var req = new CertificateRequest(CertSubject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: false));
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid(ServerAuthOid)], critical: false));

        using var ephemeral = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(10));
        // Export then reload, persist the key into Windows CNG store,
        // (making it non-ephemeral) so SslStream can use it for TLS
        var pfxBytes = ephemeral.Export(X509ContentType.Pfx);
        Directory.CreateDirectory(Path.GetDirectoryName(CertPath)!);
        File.WriteAllBytes(CertPath, pfxBytes);
        return X509CertificateLoader.LoadPkcs12(pfxBytes, password: null, LoadFlags);
    }
}
