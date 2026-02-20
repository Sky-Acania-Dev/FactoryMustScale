namespace FactoryMustScale.Simulation
{
    public static class SimMutations
    {
        public static bool TrySetCell(ref Layer layer, int x, int y, int stateId, int variantId, uint flags, int tickIndex, ref SimEventBuffer events)
        {
            if (layer == null || !layer.TryGet(x, y, out GridCellData before))
            {
                return false;
            }

            if (!layer.TrySetCellState(x, y, stateId, variantId, flags, tickIndex, out _))
            {
                return false;
            }

            int index = ((y - layer.MinY) * layer.Width) + (x - layer.MinX);
            SimEventId eventId;
            if (before.StateId == (int)GridStateId.Empty && stateId != (int)GridStateId.Empty)
            {
                eventId = SimEventId.CellCreated;
            }
            else if (before.StateId != (int)GridStateId.Empty && stateId == (int)GridStateId.Empty)
            {
                eventId = SimEventId.CellRemoved;
            }
            else
            {
                eventId = SimEventId.CellRotated;
            }

            events.RecordApplied(new SimEvent
            {
                Id = eventId,
                Tick = tickIndex,
                SourceKind = SimEventEndpointKind.Cell,
                SourceIndex = index,
                ValueA = stateId,
                ValueB = variantId,
            });

            return true;
        }

        public static bool TryGenerateItem(Layer layer, int payloadChannel, int cellIndex, int tickIndex, int itemType, ref SimEventBuffer events)
        {
            if (!TrySetPayloadByIndex(layer, payloadChannel, cellIndex, itemType))
            {
                return false;
            }

            events.RecordApplied(new SimEvent
            {
                Id = SimEventId.ItemGenerated,
                Tick = tickIndex,
                SourceKind = SimEventEndpointKind.Cell,
                SourceIndex = cellIndex,
                ItemType = itemType,
                ItemCount = 1,
            });

            return true;
        }

        public static bool QueueTransportForNextTick(int tickIndex, int sourceIndex, int targetIndex, SimEventEndpointKind sourceKind, SimEventEndpointKind targetKind, int itemType, ref SimEventBuffer events)
        {
            return events.QueueForNextTick(new SimEvent
            {
                Id = SimEventId.ItemTransported,
                Tick = tickIndex,
                SourceKind = sourceKind,
                TargetKind = targetKind,
                SourceIndex = sourceIndex,
                TargetIndex = targetIndex,
                ItemType = itemType,
                ItemCount = 1,
            });
        }

        public static bool TryApplyTransport(Layer layer, int payloadChannel, in SimEvent simEvent, int tickIndex, ref SimEventBuffer events)
        {
            if (simEvent.Id != SimEventId.ItemTransported)
            {
                return false;
            }

            if (simEvent.SourceKind != SimEventEndpointKind.Cell || simEvent.TargetKind != SimEventEndpointKind.Cell)
            {
                return false;
            }

            if (!TryGetPayloadByIndex(layer, payloadChannel, simEvent.SourceIndex, out int payload) || payload == 0)
            {
                return false;
            }

            if (!TryGetPayloadByIndex(layer, payloadChannel, simEvent.TargetIndex, out int targetPayload) || targetPayload != 0)
            {
                return false;
            }

            if (!TrySetPayloadByIndex(layer, payloadChannel, simEvent.TargetIndex, payload)
                || !TrySetPayloadByIndex(layer, payloadChannel, simEvent.SourceIndex, 0))
            {
                return false;
            }

            SimEvent applied = simEvent;
            applied.Tick = tickIndex;
            applied.ItemType = payload;
            events.RecordApplied(applied);
            return true;
        }

        public static bool TryMutateItem(Layer layer, int payloadChannel, int cellIndex, int tickIndex, int newItemType, ref SimEventBuffer events)
        {
            if (!TrySetPayloadByIndex(layer, payloadChannel, cellIndex, newItemType))
            {
                return false;
            }

            events.RecordApplied(new SimEvent
            {
                Id = SimEventId.ItemMutated,
                Tick = tickIndex,
                SourceKind = SimEventEndpointKind.Cell,
                SourceIndex = cellIndex,
                ItemType = newItemType,
                ItemCount = 1,
            });

            return true;
        }

        public static bool TryStoreItem(Layer layer, int payloadChannel, int cellIndex, int storageId, int tickIndex, int[] storageCounts, ref SimEventBuffer events)
        {
            if (!TryGetPayloadByIndex(layer, payloadChannel, cellIndex, out int payload) || payload == 0)
            {
                return false;
            }

            if (storageCounts != null && storageId >= 0 && storageId < storageCounts.Length)
            {
                storageCounts[storageId] += 1;
            }

            if (!TrySetPayloadByIndex(layer, payloadChannel, cellIndex, 0))
            {
                return false;
            }

            events.RecordApplied(new SimEvent
            {
                Id = SimEventId.ItemStored,
                Tick = tickIndex,
                SourceKind = SimEventEndpointKind.Cell,
                TargetKind = SimEventEndpointKind.Storage,
                SourceIndex = cellIndex,
                TargetIndex = storageId,
                ItemType = payload,
                ItemCount = 1,
            });

            return true;
        }

        private static bool TryGetPayloadByIndex(Layer layer, int payloadChannel, int cellIndex, out int payload)
        {
            payload = 0;
            if (layer == null)
            {
                return false;
            }

            int x = layer.MinX + (cellIndex % layer.Width);
            int y = layer.MinY + (cellIndex / layer.Width);
            return layer.TryGetPayload(x, y, payloadChannel, out payload);
        }

        private static bool TrySetPayloadByIndex(Layer layer, int payloadChannel, int cellIndex, int payload)
        {
            if (layer == null)
            {
                return false;
            }

            int x = layer.MinX + (cellIndex % layer.Width);
            int y = layer.MinY + (cellIndex / layer.Width);
            return layer.TrySetPayload(x, y, payloadChannel, payload);
        }
    }
}
