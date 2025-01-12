using HarmonyLib;
using Unity.Netcode;

namespace ReadyCompany.Patches
{
    internal class JoinPatches
    {
        internal static void Init()
        {
            var methodInfo =
                AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.SyncAlreadyHeldObjectsServerRpc));

            if (Utils.TryGetRpcID(methodInfo, out var id))
            {
                var harmonyTarget = AccessTools.Method(typeof(StartOfRound), $"__rpc_handler_{id}");
                var harmonyFinalizer = AccessTools.Method(typeof(JoinPatches), nameof(ClientConnectionCompleted1));
                ReadyCompany.Harmony!.Patch(harmonyTarget, null, null, null, new HarmonyMethod(harmonyFinalizer), null);
            }
        }

        private static void ClientConnectionCompleted1(NetworkBehaviour target, __RpcParams rpcParams)
        {
            var startOfRound = (StartOfRound)target;
            if (!startOfRound.IsServer)
                return;

            var clientId = rpcParams.Server.Receive.SenderClientId;
            ReadyHandler.OnClientConnected();
        }
    }
}