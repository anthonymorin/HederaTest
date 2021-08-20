//#define Create
//#define Associateother
//#define SendTokens

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hashgraph;

namespace HederaTest
{
    partial class Program
    {
        static long 
            accountId, //value assigned in Program.Secrets.cs static ctor
            otherAccountId, //define this before sending tokens
            defaultToken; //value assigned in Program.Secrets.cs static ctor
        static string
            publicKey, //value assigned in Program.Secrets.cs static ctor
            privateKey, //value assigned in Program.Secrets.cs static ctor
            otherPrivateKey, //define this before associating account, if we need to
            gateway = "0.testnet.hedera.com:50211";

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");


            long token
#if Create
            = CreateExampleToken().Result;
#else
            = defaultToken;
#endif

            QueryBalance(Program.accountId).Wait();

            GetTokenInfo(token).Wait();

#if Associateother
            AssociateAccount(token, otherAccountId, Program.privateKey).Wait();
#endif

#if SendTokens
            QueryBalance(Program.otherAccountId).Wait();

            SendTokens(token, Program.accountId, Program.privateKey, Program.otherAccountId, 100).Wait();

            QueryBalance(Program.otherAccountId).Wait();
#endif
        }

        static async Task QueryBalance(long AccountId)
        {
            try
            {
                Address address = new Address(0, 0, AccountId);

                Gateway gateway = new Gateway(Program.gateway, 0, 0, 3);

                await using Client client = new Client(ctx => { ctx.Gateway = gateway; });

                AccountBalances balances = await client.GetAccountBalancesAsync(address);

                Console.WriteLine($"Account 0.0.{address.AccountNum}");

                Console.WriteLine($"Crypto Balance is {balances.Crypto / 100_000_000:#,##0.0} hBars.");

                foreach (KeyValuePair<Address, CryptoBalance> token in balances.Tokens)
                {
                    Console.WriteLine($"Token 0.0.{token.Key.AccountNum} is {token.Value:#,##0.0}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
            }

            Console.WriteLine();
        }

        static async Task GetTokenInfo(long TokenId)
        {
            try
            {
                Gateway 
                    gateway = new Gateway(Program.gateway, 0, 0, 3);
                Address 
                    token = new Address(0, 0, TokenId);
                Address 
                    payer = new Address(0, 0, Program.accountId);
                Signatory 
                    payerSignatory = new Signatory(Hex.ToBytes(Program.privateKey));

                await using Client
                    client = new Client(ctx =>
                    {
                        ctx.Gateway = gateway;
                        ctx.Payer = payer;
                        ctx.Signatory = payerSignatory;
                    });

                TokenInfo 
                    info = await client.GetTokenInfoAsync(token);

                Console.WriteLine($"Token: 0.0.{info.Token.AccountNum}");
                Console.WriteLine($"Symbol: {info.Symbol}");
                Console.WriteLine($"Name: {info.Name}");
                Console.WriteLine($"Treasury: 0.0.{info.Treasury.AccountNum}");
                Console.WriteLine($"Circulation: {info.Circulation:#,##0.0}");
                Console.WriteLine($"Decimals: {info.Decimals}");
                Console.WriteLine($"Administrator: {Hex.FromBytes(info.Administrator.PublicKey)}");
                Console.WriteLine($"GrantKycEndorsement: {(info.GrantKycEndorsement == null ? "Null" : "Not Null")}");
                Console.WriteLine($"SuspendEndorsement: {Hex.FromBytes(info.SuspendEndorsement.PublicKey)}");
                Console.WriteLine($"ConfiscateEndorsement: {Hex.FromBytes(info.ConfiscateEndorsement.PublicKey)}");
                Console.WriteLine($"SupplyEndorsement: {Hex.FromBytes(info.SupplyEndorsement.PublicKey)}");
                Console.WriteLine($"Tradable Status: {info.TradableStatus}");
                Console.WriteLine($"KYC Status: {info.KycStatus}");
                Console.WriteLine($"Expiration: {info.Expiration}");
                Console.WriteLine($"Renew Period: {info.RenewPeriod}");
                Console.WriteLine($"Renew Account: 0.0.{info.RenewAccount.AccountNum}");
                Console.WriteLine($"Deleted: {info.Deleted}");

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                Console.Error.WriteLine(ex.StackTrace);
            }

            Console.WriteLine();
        }

        static async Task<long> CreateExampleToken()
        {
            DateTime
                startTime = DateTime.Now;

            try
            {
                Gateway 
                    gateway = new Gateway(Program.gateway, 0, 0, 3);
                Address 
                    payer = new Address(0, 0, Program.accountId);
                Signatory 
                    payerSignatory = new Signatory(Hex.ToBytes(privateKey));
                Endorsement 
                    tokenEndorsement = new Endorsement(Hex.ToBytes(publicKey));
                Signatory 
                    tokenSignatory = new Signatory(Hex.ToBytes(privateKey));
                CreateTokenParams 
                    createParams = new CreateTokenParams
                    {
                        Name = "Anthony test coin",
                        Symbol = "AMTEST",
                        Circulation = 1_500_000_000,
                        Decimals = 0,
                        Treasury = payer,
                        Administrator = tokenEndorsement,
                        GrantKycEndorsement = null,
                        SuspendEndorsement = tokenEndorsement,
                        ConfiscateEndorsement = tokenEndorsement,
                        SupplyEndorsement = tokenEndorsement,
                        InitializeSuspended = false,
                        Expiration = DateTime.UtcNow.AddDays(90),
                        RenewAccount = payer,
                        RenewPeriod = TimeSpan.FromDays(90),
                        Signatory = tokenSignatory, 
                        Memo="", //must define this as non-null or CreateTokenAsync throws an exception
                    };

                await using Client 
                    client = new Client(ctx =>
                    {
                        ctx.Gateway = gateway;
                        ctx.Payer = payer;
                        ctx.Signatory = payerSignatory;
                    });

                CreateTokenReceipt receipt = await client.CreateTokenAsync(createParams);

                Console.WriteLine($"Token Created with ID 0.0.{receipt.Token.AccountNum}");

                Console.WriteLine($"Elapsed time: {DateTime.Now - startTime}");

                return receipt.Token.AccountNum;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                Console.Error.WriteLine(ex.StackTrace);

                return -1;
            }
            finally
            {
                Console.WriteLine();
            }
        }

        static async Task AssociateAccount(long TokenId, long AccountId, string AccountSignatory)
        {
            try
            {
                Gateway 
                    gateway = new Gateway(Program.gateway, 0, 0, 3);
                Address 
                    payer = new Address(0, 0, Program.accountId);
                Address 
                    token = new Address(0, 0, TokenId);
                Address 
                    account = new Address(0, 0, AccountId);
                Signatory 
                    payerSignatory = new Signatory(Hex.ToBytes(Program.privateKey));
                Signatory 
                    accountSignatory = new Signatory(Hex.ToBytes(AccountSignatory));

                await using Client 
                    client = new Client(ctx =>
                    {
                        ctx.Gateway = gateway;
                        ctx.Payer = payer;
                        ctx.Signatory = payerSignatory;
                    });

                TransactionReceipt 
                    receipt = await client.AssociateTokenAsync(token, account, accountSignatory);

                Console.WriteLine($"Token associate status returned status: {receipt.Status}");

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                Console.Error.WriteLine(ex.StackTrace);
            }
            finally
            {
                Console.WriteLine();
            }
        }

        static async Task SendTokens(long TokenId, long SourceAccountId, string SourceAccountSignatory, long DestinationAccountId, long Amount)
        {
            try
            {
                Gateway 
                    gateway = new Gateway(Program.gateway, 0, 0, 3);
                Address 
                    token = new Address(0, 0, TokenId);
                Address 
                    fromAccount = new Address(0, 0, SourceAccountId);
                Signatory 
                    fromSignatory = new Signatory(Hex.ToBytes(SourceAccountSignatory));
                Address 
                    toAccount = new Address(0, 0, DestinationAccountId);

                await using Client 
                    client = new Client(ctx =>
                    {
                        ctx.Gateway = gateway;
                        ctx.Payer = fromAccount;
                        ctx.Signatory = fromSignatory;
                    });
                TransactionReceipt 
                    receipt = await client.TransferTokensAsync(token, fromAccount, toAccount, Amount);

                Console.WriteLine($"Token transfer returned status: {receipt.Status}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);

                Console.Error.WriteLine(ex.StackTrace);
            }
            finally
            {
                Console.WriteLine();
            }
        }

    }
}
