using System;
using StoreKit;
using System.Collections.Generic;
using Foundation;
using System.Runtime.InteropServices;

namespace osum.GameModes.Store
{
    public class StoreModeIphone : StoreMode
    {
        InAppPurchaseManager iap = new InAppPurchaseManager();

        public StoreModeIphone()
        {
            Console.WriteLine("hello?");
        }

        public override void Initialize()
        {
            base.Initialize();
            iap = new InAppPurchaseManager();
        }

        public override void RestorePurchases()
        {
            GameBase.GloballyDisableInput = true;
            iap.RestorePurchases(purchaseCompleteResponse);
        }

        public override void Dispose ()
        {
            base.Dispose();
            if (iap != null) iap.Dispose();
        }

        protected override void handlePackInfo(string result, Exception e)
        {
            base.handlePackInfo(result, e);

            if (packs.Count > 0)
            {
                //grab information for any packs which require payment.
                List<string> ids = new List<string>();
                foreach (PackPanel pp in packs)
                {
                    if (!pp.IsFree)
                        ids.Add(pp.PackId);
                }

                if (ids.Count > 0)
                    iap.RequestProductData(ids, productsResponse);
            }
        }

        protected override void purchase(PackPanel pack)
        {
            if (pack.IsFree)
                download(pack);
            else
            {
                GameBase.GloballyDisableInput = true;
                iap.PurchaseItem(pack.PackId, purchaseCompleteResponse);
            }
        }

        void purchaseCompleteResponse(SKPaymentTransaction transaction, bool wasSuccessful)
        {
            GameBase.GloballyDisableInput = false;

            if (transaction == null) return;

            PackPanel pack = packs.Find(p => p.PackId == transaction.Payment.ProductIdentifier);
            if (pack == null) return;

            if (wasSuccessful)
            {
#if !DIST
                Console.WriteLine("Receipt is: ");

                foreach (byte b in transaction.TransactionReceipt)
                    Console.Write(b.ToString());
                Console.WriteLine();
#endif

                NSData receiptRaw = transaction.TransactionReceipt;

                if (receiptRaw == null || receiptRaw.Bytes == null)
                    wasSuccessful = false;
                else
                {
                    byte[] dataBytes = new byte[receiptRaw.Length];
                    Marshal.Copy(receiptRaw.Bytes, dataBytes, 0, Convert.ToInt32(receiptRaw.Length));

                    pack.Receipt = dataBytes;

                    download(pack);
                }
            }

            if (!wasSuccessful && transaction.Error != null && transaction.Error.Code != 2)
                GameBase.Notify(transaction.Error.ToString(), null);
        }

        void productsResponse(SKProduct[] products)
        {
            GameBase.Scheduler.Add(delegate {
                foreach (SKProduct p in products)
                {
                    PackPanel associatedPack = packs.Find(pack => pack.PackId == p.ProductIdentifier);
                    if (associatedPack != null)
                        associatedPack.SetPrice(p.LocalizedPrice());
                }
            });
        }
    }
}

