export interface InventoryDto {
  inventoryItems: ItemDto[];
}

export interface ItemDto {
  itemId: string;
  quantity: number;
}
