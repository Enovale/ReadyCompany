using System.Reflection;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using Unity.Netcode;
using UnityEngine;

namespace ReadyCompany.Util
{
    public static class Utils
    {
        private static readonly MethodInfo BeginSendClientRpc =
            AccessTools.Method(typeof(NetworkBehaviour), nameof(NetworkBehaviour.__beginSendClientRpc));
        private static readonly MethodInfo BeginSendServerRpc =
            AccessTools.Method(typeof(NetworkBehaviour), nameof(NetworkBehaviour.__beginSendServerRpc));
        internal static bool TryGetRpcID(MethodInfo methodInfo, out uint rpcID)
        {
            var instructions = methodInfo.GetMethodPatcher().CopyOriginal().Definition.Body.Instructions;

            rpcID = 0;
            for (var i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].OpCode == OpCodes.Ldc_I4 && instructions[i - 1].OpCode == OpCodes.Ldarg_0)
                    rpcID = (uint)(int)instructions[i].Operand;

                if (instructions[i].OpCode != OpCodes.Call ||
                    instructions[i].Operand is not MethodReference operand ||
                    !(operand.Is(BeginSendClientRpc) || operand.Is(BeginSendServerRpc)))
                    continue;
            
                ReadyCompany.Logger.LogDebug($"Rpc Id found for {methodInfo.Name}: {rpcID}U");
                return true;
            }

            ReadyCompany.Logger.LogFatal($"Cannot find Rpc ID for {methodInfo.Name}");
            return false;
        }

        internal static void PlayRandomClip(AudioSource audioSource, AudioClip[] clipsArray, float oneShotVolume = 1f)
        {
            var index = Random.Range(0, Mathf.Min(1000, clipsArray.Length));
            audioSource.PlayOneShot(clipsArray[index], oneShotVolume);
        }
    }
}