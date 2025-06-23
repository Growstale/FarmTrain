using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ShopItemRow : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI availableText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI buttonText;

    private Action<ShopItem> onActionButtonClicked;
    private ShopItem currentShopItem;

  
    public void Setup(ShopItem shopItem, int shopStock, int playerItemCount, bool isBuyMode, Action<ShopItem> buttonCallback)
    {
        this.currentShopItem = shopItem;
        this.onActionButtonClicked = buttonCallback;
        var itemData = shopItem.itemData;

        itemIcon.sprite = itemData.itemIcon;
        descriptionText.text = itemData.description;

        if (isBuyMode)
        {
            buttonText.text = "Buy";
            priceText.text = $"{shopItem.buyPrice} BYN";
            availableText.text = shopItem.isInfiniteStock ? "In stock" : $"{shopStock}";

            bool canAfford = PlayerWallet.Instance.HasEnoughMoney(shopItem.buyPrice);
            bool hasStock = shopItem.isInfiniteStock || shopStock > 0;
            bool isUpgradeAlreadyOwned = (itemData.itemType == ItemType.Tool) && TrainUpgradeManager.Instance.HasUpgrade(itemData);

            actionButton.interactable = canAfford && hasStock && !isUpgradeAlreadyOwned;
        }
        else 
        {
            buttonText.text = "Sell";
            priceText.text = $"{shopItem.sellPrice} BYN";

            availableText.text = $"You have: {playerItemCount}";

            bool playerHasItem = playerItemCount > 0;
            actionButton.interactable = playerHasItem && shopItem.willBuy;
        }

        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        onActionButtonClicked?.Invoke(currentShopItem);
    }
}