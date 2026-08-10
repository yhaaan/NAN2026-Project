using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace NAN2026.Gomoku
{
    public enum PvpMatchCommandType : byte
    {
        Place,
        Reroll,
        Continue
    }

    public readonly struct PvpMatchCommand
    {
        public PvpMatchCommandType Type { get; }
        public StoneColor Side { get; }
        public int X { get; }
        public int Y { get; }
        public int OfferIndex { get; }

        public PvpMatchCommand(
            PvpMatchCommandType type,
            StoneColor side = StoneColor.None,
            int x = 0,
            int y = 0,
            int offerIndex = -1)
        {
            Type = type;
            Side = side;
            X = x;
            Y = y;
            OfferIndex = offerIndex;
        }
    }

    /// <summary>
    /// Keeps the Relay connection alive between scenes and exchanges the small,
    /// deterministic command stream used by the PvP prototype.
    /// </summary>
    public sealed class PvpMatchCoordinator : MonoBehaviour
    {
        private const string ReadyMessage = "nan2026.pvp.ready";
        private const string SetupMessage = "nan2026.pvp.setup";
        private const string RequestMessage = "nan2026.pvp.request";
        private const string CommandMessage = "nan2026.pvp.command";
        private const int MessageCapacity = 64;
        private const float ReadyRetryInterval = 1f;

        private NetworkManager networkManager;
        private Action<int> setupReceived;
        private bool handlersRegistered;
        private bool matchPending;
        private bool seedAvailable;
        private int matchSeed;
        private float nextReadySendTime;

        public static PvpMatchCoordinator Instance { get; private set; }
        public bool IsMatchPending => matchPending;
        public bool IsHost => networkManager != null && networkManager.IsHost;
        public StoneColor LocalSide => IsHost ? StoneColor.Black : StoneColor.White;

        public static StoneColor GetSideForClient(ulong clientId)
        {
            return clientId == NetworkManager.ServerClientId
                ? StoneColor.Black
                : StoneColor.White;
        }

        public event Action<ulong, PvpMatchCommand> CommandRequested;
        public event Action<PvpMatchCommand> CommandReceived;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            networkManager = GetComponent<NetworkManager>();
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            EnsureHandlersRegistered();
            TrySendReadyMessage();
        }

        private void OnDestroy()
        {
            UnregisterHandlers();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PrepareForGame(bool hosting)
        {
            matchPending = true;
            setupReceived = null;
            seedAvailable = hosting;
            nextReadySendTime = 0f;
            if (hosting)
            {
                matchSeed = unchecked(Environment.TickCount ^ Guid.NewGuid().GetHashCode());
            }

            EnsureHandlersRegistered();
        }

        public void BeginSynchronization(Action<int> onSetupReceived)
        {
            setupReceived = onSetupReceived;
            EnsureHandlersRegistered();

            if (seedAvailable)
            {
                setupReceived?.Invoke(matchSeed);
                return;
            }

            TrySendReadyMessage();
        }

        public void EndSynchronization()
        {
            setupReceived = null;
            CommandRequested = null;
            CommandReceived = null;
        }

        public void RequestCommand(PvpMatchCommand command)
        {
            if (!matchPending || networkManager == null || !networkManager.IsListening)
            {
                return;
            }

            if (networkManager.IsHost)
            {
                CommandRequested?.Invoke(networkManager.LocalClientId, command);
                return;
            }

            SendCommand(RequestMessage, NetworkManager.ServerClientId, command);
        }

        public void ApproveCommand(PvpMatchCommand command)
        {
            if (!IsHost || !handlersRegistered)
            {
                return;
            }

            using var writer = new FastBufferWriter(MessageCapacity, Allocator.Temp);
            WriteCommand(writer, command);
            networkManager.CustomMessagingManager.SendNamedMessageToAll(
                CommandMessage,
                writer,
                NetworkDelivery.ReliableSequenced);
        }

        private void EnsureHandlersRegistered()
        {
            if (handlersRegistered
                || networkManager == null
                || !networkManager.IsListening
                || networkManager.CustomMessagingManager == null)
            {
                return;
            }

            CustomMessagingManager messaging = networkManager.CustomMessagingManager;
            messaging.RegisterNamedMessageHandler(ReadyMessage, HandleReadyMessage);
            messaging.RegisterNamedMessageHandler(SetupMessage, HandleSetupMessage);
            messaging.RegisterNamedMessageHandler(RequestMessage, HandleRequestMessage);
            messaging.RegisterNamedMessageHandler(CommandMessage, HandleCommandMessage);
            handlersRegistered = true;
        }

        private void UnregisterHandlers()
        {
            if (!handlersRegistered || networkManager == null || networkManager.CustomMessagingManager == null)
            {
                return;
            }

            CustomMessagingManager messaging = networkManager.CustomMessagingManager;
            messaging.UnregisterNamedMessageHandler(ReadyMessage);
            messaging.UnregisterNamedMessageHandler(SetupMessage);
            messaging.UnregisterNamedMessageHandler(RequestMessage);
            messaging.UnregisterNamedMessageHandler(CommandMessage);
            handlersRegistered = false;
        }

        private void HandleReadyMessage(ulong senderClientId, FastBufferReader reader)
        {
            if (!IsHost || !matchPending || !seedAvailable)
            {
                return;
            }

            using var writer = new FastBufferWriter(MessageCapacity, Allocator.Temp);
            writer.WriteValueSafe(matchSeed);
            networkManager.CustomMessagingManager.SendNamedMessage(
                SetupMessage,
                senderClientId,
                writer,
                NetworkDelivery.ReliableSequenced);
        }

        private void HandleSetupMessage(ulong senderClientId, FastBufferReader reader)
        {
            if (IsHost || senderClientId != NetworkManager.ServerClientId)
            {
                return;
            }

            if (seedAvailable)
            {
                return;
            }

            reader.ReadValueSafe(out matchSeed);
            seedAvailable = true;
            setupReceived?.Invoke(matchSeed);
        }

        private void HandleRequestMessage(ulong senderClientId, FastBufferReader reader)
        {
            if (!IsHost || !matchPending)
            {
                return;
            }

            CommandRequested?.Invoke(senderClientId, ReadCommand(reader));
        }

        private void HandleCommandMessage(ulong senderClientId, FastBufferReader reader)
        {
            if (senderClientId != NetworkManager.ServerClientId)
            {
                return;
            }

            CommandReceived?.Invoke(ReadCommand(reader));
        }

        private void TrySendReadyMessage()
        {
            if (IsHost
                || seedAvailable
                || setupReceived == null
                || !handlersRegistered
                || Time.unscaledTime < nextReadySendTime)
            {
                return;
            }

            nextReadySendTime = Time.unscaledTime + ReadyRetryInterval;
            SendEmptyMessage(ReadyMessage, NetworkManager.ServerClientId);
        }
        private void SendEmptyMessage(string messageName, ulong clientId)
        {
            if (!handlersRegistered)
            {
                return;
            }

            using var writer = new FastBufferWriter(1, Allocator.Temp);
            networkManager.CustomMessagingManager.SendNamedMessage(
                messageName,
                clientId,
                writer,
                NetworkDelivery.ReliableSequenced);
        }

        private void SendCommand(string messageName, ulong clientId, PvpMatchCommand command)
        {
            if (!handlersRegistered)
            {
                return;
            }

            using var writer = new FastBufferWriter(MessageCapacity, Allocator.Temp);
            WriteCommand(writer, command);
            networkManager.CustomMessagingManager.SendNamedMessage(
                messageName,
                clientId,
                writer,
                NetworkDelivery.ReliableSequenced);
        }

        private static void WriteCommand(FastBufferWriter writer, PvpMatchCommand command)
        {
            writer.WriteValueSafe((byte)command.Type);
            writer.WriteValueSafe((byte)command.Side);
            writer.WriteValueSafe(command.X);
            writer.WriteValueSafe(command.Y);
            writer.WriteValueSafe(command.OfferIndex);
        }

        private static PvpMatchCommand ReadCommand(FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte type);
            reader.ReadValueSafe(out byte side);
            reader.ReadValueSafe(out int x);
            reader.ReadValueSafe(out int y);
            reader.ReadValueSafe(out int offerIndex);
            return new PvpMatchCommand(
                (PvpMatchCommandType)type,
                (StoneColor)side,
                x,
                y,
                offerIndex);
        }
    }
}
