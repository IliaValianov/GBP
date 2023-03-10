using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Fabricator : Puzzle
{
	[SerializeField] private PlayerController player;
	[SerializeField] private Camera mainCamera;
	[SerializeField] private List<ItemSlot> slots;
	[SerializeField] private ItemSlot fabricatedItem;
	[SerializeField] private Animation fabricatorAnimation;
	
	private VariableSystem _variableSystem;
	private Receipt _receipt;
	[Header("UI")] 
	[SerializeField] private Button runButton;
	[SerializeField] private Button backButton;
	//[SerializeField] private NoteInfo debugReceipt;
	[SerializeField] private SelectReceipt receiptButtonTemplate;
	[SerializeField] private Transform receiptsRoot;

	private readonly List<SelectReceipt> _receiptButtons = new List<SelectReceipt>();


	private InventoryToFabricatorBridge _intToFabBridge;
	private FabricatorToInventoryBridge _fabToInvBridge;
	
	public bool IsProcessing { get; private set; }

	private void Awake()
	{
		runButton.onClick.AddListener(Run);
		backButton.onClick.AddListener(Hide);
	}

// 	private void Start()
// 	{
// #if DEBUG
// 		LoadReceipt(debugReceipt.receipt);
// #endif
// 	}

	private void Initialize()
	{
		_intToFabBridge = new InventoryToFabricatorBridge(this, _variableSystem.Inventory, _variableSystem);
		_fabToInvBridge = new FabricatorToInventoryBridge(this, _variableSystem.Inventory, _variableSystem);
		foreach (ItemSlot itemSlot in slots)
		{
			itemSlot.ItemHandler = _fabToInvBridge;
		}

		fabricatedItem.ItemHandler = _fabToInvBridge;
	}

	private void LoadReceipts()
	{
		ClearReceipts();
		foreach (NoteInfo noteInfo in _variableSystem.NoteBook.Notes)
		{
			if (!noteInfo.isReceipt) 
				continue;
			var view = Instantiate(receiptButtonTemplate, receiptsRoot);
			view.Setup(this, noteInfo.receipt);
			_receiptButtons.Add(view);
		}

		if (_receiptButtons.Count > 0)
		{
			LoadReceipt(_receiptButtons[0].Receipt);
		}
	}

	private void ClearReceipts()
	{
		foreach (SelectReceipt receiptButton in _receiptButtons)
		{
			Destroy(receiptButton.gameObject);
		}
		_receiptButtons.Clear();
	}

	private void Update()
	{
		backButton.interactable = !IsProcessing;
		runButton.interactable = !IsProcessing;

		foreach (SelectReceipt receiptButton in _receiptButtons)
		{
			receiptButton.UpdateView(_receipt);
		}
	}

	public void LoadReceipt(Receipt receipt)
	{
		_receipt = receipt;
	}

	private void Run()
	{
		if (_receipt == null)
			//TODO play something;
			return;
		
		var match = new bool[_receipt.requiredItems.Count];
		for (int i = 0; i < _receipt.requiredItems.Count; i++)
		{
			var requiredItem = _receipt.requiredItems[i];
			foreach (ItemSlot itemSlot in slots)
			{
				if (itemSlot.ItemInfo == requiredItem)
				{
					match[i] = true;
					break;
				}
			}
		}

		var allMatch = true;
		for (int i = 0; i < match.Length; i++)
		{
			if (match[i] == false)
			{
				allMatch = false;
			}
		}

		if (!allMatch)
		{
			//TODO play something;
		}
		else
		{
			StartCoroutine(AssembleItem());
		}
		Debug.Log($"[Fabricator] {_receipt.screenName} processing: {IsProcessing.ToString()}");
	}

	private IEnumerator AssembleItem()
	{
		IsProcessing = true;
		foreach (ItemSlot slot in slots)
		{
			if (slot.isFull)
			{
				slot.isFull = false;
				slot.SetItem(null);
			}
		}

		if (fabricatorAnimation != null)
		{
			fabricatorAnimation.Play();
            AudioManager.instance.InitializefabricatorAnim();
        }
		
		yield return new WaitForSeconds(_receipt.fabricationTime);
		fabricatedItem.SetItem(_receipt.result); 
		IsProcessing = false;
	}

	public override void Show()
	{
		base.Show();
		_variableSystem = VariableSystem.Instance;
		Initialize();
		LoadReceipts();
		player.gameObject.SetActive(false);
		mainCamera.gameObject.SetActive(false);
		_variableSystem.Inventory.SetItemHandler(_intToFabBridge);
	}
	
	public override void Hide()
	{
		if (IsProcessing)
			return;
		
		foreach (ItemSlot slot in slots)
		{
			if (slot.ItemInfo != null)
			{
				_fabToInvBridge.ProcessItem(slot.ItemInfo);
			}
		}

		if (fabricatedItem.ItemInfo != null)
		{
			_fabToInvBridge.ProcessItem(fabricatedItem.ItemInfo);
		}
		_variableSystem.Inventory.SetItemHandler(null);
		player.gameObject.SetActive(true);
		mainCamera.gameObject.SetActive(true);
		base.Hide();
	}

	public bool AddIngredient(ItemInfo itemInfo)
	{
		foreach (ItemSlot slot in slots)
		{
			if (slot.isFull) 
				continue;
			slot.isFull = true;
			slot.SetItem(itemInfo);
			return true;
		}

		return false;
	}
	
	public bool RemoveIngredient(ItemInfo itemInfo)
	{
		foreach (ItemSlot slot in slots)
		{
			if (slot.isFull && slot.ItemInfo == itemInfo)
			{
				slot.isFull = false;
				slot.SetItem(null);
				return true;
			}
		}

		return false;
	}

	public bool RemoveResult(ItemInfo itemInfo)
	{
		if (fabricatedItem.isFull && fabricatedItem.ItemInfo == itemInfo)
		{
			fabricatedItem.SetItem(null);
			return true;
		}

		return false;
	}
}
