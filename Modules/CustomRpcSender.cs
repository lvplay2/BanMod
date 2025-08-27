using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using InnerNet;
using System;
using System.Linq;

namespace BanMod;

public class CustomRpcSender
{
    public MessageWriter stream;
    public readonly string name;
    public readonly SendOption sendOption;
    public bool isUnsafe;
    public delegate void onSendDelegateType();
    public onSendDelegateType onSendDelegate;
    public const byte RpcImpostorNameSync = 70;
    public State CurrentState
    {
        get { return currentState; }
        set
        {
            if (isUnsafe) currentState = value;
            else Logger.Warn("CurrentStateはisUnsafeがtrueの時のみ上書きできます", "CustomRpcSender");
        }
    }
    public const byte CustomRpcId = 200; // ID RPC custom

    private State currentState = State.BeforeInit;

    //0~: targetClientId (GameDataTo)
    //-1: 全プレイヤー (GameData)
    //-2: 未設定
    private int currentRpcTarget;

    private CustomRpcSender() { }
    public CustomRpcSender(string name, SendOption sendOption, bool isUnsafe)
    {
        stream = MessageWriter.Get(sendOption);

        this.name = name;
        this.sendOption = sendOption;
        this.isUnsafe = isUnsafe;
        this.currentRpcTarget = -2;
        onSendDelegate = () => Logger.Info($"{this.name}'s onSendDelegate =>", "CustomRpcSender");

        currentState = State.Ready;
        Logger.Info($"\"{name}\" is ready", "CustomRpcSender");
    }
    public static CustomRpcSender Create(string name = "No Name Sender", SendOption sendOption = SendOption.None, bool isUnsafe = false)
    {
        return new CustomRpcSender(name, sendOption, isUnsafe);
    }

    #region Start/End Message
    public CustomRpcSender StartMessage(int targetClientId = -1)
    {
        if (currentState != State.Ready)
        {
            string errorMsg = $"Messageを開始しようとしましたが、StateがReadyではありません (in: \"{name}\")";
            if (isUnsafe)
            {
                Logger.Warn(errorMsg, "CustomRpcSender.Warn");
            }
            else
            {
                throw new InvalidOperationException(errorMsg);
            }
        }

        if (targetClientId < 0)
        {
            // 全員に対するRPC
            stream.StartMessage(5);
            stream.Write(AmongUsClient.Instance.GameId);
        }
        else
        {
            // 特定のクライアントに対するRPC (Desync)
            stream.StartMessage(6);
            stream.Write(AmongUsClient.Instance.GameId);
            stream.WritePacked(targetClientId);
        }

        currentRpcTarget = targetClientId;
        currentState = State.InRootMessage;
        return this;
    }
    public CustomRpcSender EndMessage()
    {
        if (currentState != State.InRootMessage)
        {
            string errorMsg = $"Messageを終了しようとしましたが、StateがInRootMessageではありません (in: \"{name}\")";
            if (isUnsafe)
                Logger.Warn(errorMsg, "CustomRpcSender.Warn");
            else
                throw new InvalidOperationException(errorMsg);
        }
        stream.EndMessage();

        currentRpcTarget = -2;
        currentState = State.Ready;
        return this;
    }
    #endregion
    #region Start/End Rpc
    public CustomRpcSender StartRpc(uint targetNetId, RpcCalls rpcCall)
        => StartRpc(targetNetId, (byte)rpcCall);
    public CustomRpcSender StartRpc(
        uint targetNetId,
        byte callId)
    {
        if (currentState != State.InRootMessage)
        {
            string errorMsg = $"RPCを開始しようとしましたが、StateがInRootMessageではありません (in: \"{name}\")";
            if (isUnsafe)
                Logger.Warn(errorMsg, "CustomRpcSender.Warn");
            else
                throw new InvalidOperationException(errorMsg);
        }

        stream.StartMessage(2);
        stream.WritePacked(targetNetId);
        stream.Write(callId);

        currentState = State.InRpc;
        return this;
    }
    public CustomRpcSender EndRpc()
    {
        if (currentState != State.InRpc)
        {
            string errorMsg = $"RPCを終了しようとしましたが、StateがInRpcではありません (in: \"{name}\")";
            if (isUnsafe)
                Logger.Warn(errorMsg, "CustomRpcSender.Warn");
            else
                throw new InvalidOperationException(errorMsg);
        }

        stream.EndMessage();
        currentState = State.InRootMessage;
        return this;
    }
    #endregion
    public CustomRpcSender AutoStartRpc(
        uint targetNetId,
        byte callId,
        int targetClientId = -1)
    {
        if (targetClientId == -2) targetClientId = -1;
        if (currentState is not State.Ready and not State.InRootMessage)
        {
            string errorMsg = $"RPCを自動で開始しようとしましたが、StateがReadyまたはInRootMessageではありません (in: \"{name}\")";
            if (isUnsafe)
                Logger.Warn(errorMsg, "CustomRpcSender.Warn");
            else
                throw new InvalidOperationException(errorMsg);
        }
        if (currentRpcTarget != targetClientId)
        {
            //StartMessage処理
            if (currentState == State.InRootMessage) this.EndMessage();
            this.StartMessage(targetClientId);
        }
        this.StartRpc(targetNetId, callId);

        return this;
    }
    public void SendMessage()
    {
        if (currentState == State.InRootMessage)
            this.EndMessage();

        if (currentState != State.Ready)
        {
            string errorMsg = $"Tentativo di inviare un RPC, ma lo stato non è Ready (in: \"{name}\")";
            if (isUnsafe)
                Logger.Warn(errorMsg, "CustomRpcSender.Warn");
            else
                throw new InvalidOperationException(errorMsg);
        }

        if (stream == null)
        {
            Logger.Error("stream è null, impossibile inviare il messaggio.", "CustomRpcSender");
            return;
        }

        if (AmongUsClient.Instance == null)
        {
            Logger.Error("AmongUsClient.Instance è null, probabilmente il client non è ancora connesso.", "CustomRpcSender");
            return;
        }

        try
        {
            AmongUsClient.Instance.SendOrDisconnect(stream);
        }
        catch (Exception ex)
        {
            Logger.Error($"Errore durante l'invio: {ex.Message}", "CustomRpcSender");
            return;
        }

        // Proteggi contro delegate null
        if (onSendDelegate != null)
        {
            onSendDelegate();
        }
        else
        {
            Logger.Warn("onSendDelegate è null, nessuna azione post-invio eseguita.", "CustomRpcSender");
        }

        currentState = State.Finished;
        Logger.Info($"\"{name}\" completato", "CustomRpcSender");
        stream.Recycle();
    }
    // Write
    #region PublicWriteMethods
    public CustomRpcSender Write(float val) => Write(w => w.Write(val));
    public CustomRpcSender Write(string val) => Write(w => w.Write(val));
    public CustomRpcSender Write(ulong val) => Write(w => w.Write(val));
    public CustomRpcSender Write(int val) => Write(w => w.Write(val));
    public CustomRpcSender Write(uint val) => Write(w => w.Write(val));
    public CustomRpcSender Write(ushort val) => Write(w => w.Write(val));
    public CustomRpcSender Write(byte val) => Write(w => w.Write(val));
    public CustomRpcSender Write(sbyte val) => Write(w => w.Write(val));
    public CustomRpcSender Write(bool val) => Write(w => w.Write(val));
    public CustomRpcSender Write(Il2CppStructArray<byte> bytes) => Write(w => w.Write(bytes));
    public CustomRpcSender Write(Il2CppStructArray<byte> bytes, int offset, int length) => Write(w => w.Write(bytes, offset, length));
    public CustomRpcSender WriteBytesAndSize(Il2CppStructArray<byte> bytes) => Write(w => w.WriteBytesAndSize(bytes));
    public CustomRpcSender WritePacked(int val) => Write(w => w.WritePacked(val));
    public CustomRpcSender WritePacked(uint val) => Write(w => w.WritePacked(val));
    public CustomRpcSender WriteNetObject(InnerNetObject obj) => Write(w => w.WriteNetObject(obj));
    public CustomRpcSender WriteMessageType(byte val) => Write(w => w.StartMessage(val));
    public CustomRpcSender WriteEndMessage() => Write(w => w.EndMessage());
    #endregion

    private CustomRpcSender Write(Action<MessageWriter> action)
    {
        if (currentState != State.InRpc)
        {
            string errorMsg = $"RPCを書き込もうとしましたが、StateがWrite(書き込み中)ではありません (in: \"{name}\")";
            if (isUnsafe)
                Logger.Warn(errorMsg, "CustomRpcSender.Warn");
            else
                throw new InvalidOperationException(errorMsg);
        }
        action(stream);

        return this;
    }
    public enum State
    {
        BeforeInit = 0, //初期化前 何もできない
        Ready, //送信準備完了 StartMessageとSendMessageを実行可能
        InRootMessage, //StartMessage～EndMessageの間の状態 StartRpcとEndMessageを実行可能
        InRpc, //StartRpc～EndRpcの間の状態 WriteとEndRpcを実行可能
        Finished, //送信後 何もできない
    }
}