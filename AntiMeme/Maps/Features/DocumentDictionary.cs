using System;
using System.Collections.Generic;
using AntiMeme.Roles.Scps;

namespace AntiMeme.Maps.Features;

/// <summary>マップ文書の種類と本文を一元管理します。</summary>
public enum DocumentType
{
    Scp033,
    Scp096,
    Scp3005,
    Backrooms,
    Cafeteria,
    DeltaWarhead,
    OmegaWarhead,
    ScientistSamuels,
    ScientistMasoi,
    ScientistGalia,
    ScientistJlolldo,
    AboutSergey,
    AntiAntiMeme,
    AprilWtf,
    Overbeyond,
    AboutSQ,
    Kai,
    OperationDoc,
    SpecDoc,
}

public static class DocumentDictionary
{
    public static readonly DocumentType[] DefinedTypes =
    {
        DocumentType.Scp033, DocumentType.Scp096, DocumentType.Scp3005,
        DocumentType.Backrooms, DocumentType.Cafeteria, DocumentType.DeltaWarhead,
        DocumentType.OmegaWarhead, DocumentType.ScientistSamuels, DocumentType.ScientistMasoi,
        DocumentType.ScientistGalia, DocumentType.ScientistJlolldo, DocumentType.AboutSergey, DocumentType.AntiAntiMeme,
        DocumentType.AprilWtf, DocumentType.Overbeyond, DocumentType.AboutSQ,
        DocumentType.Kai, DocumentType.OperationDoc, DocumentType.SpecDoc,
    };

    private static readonly IReadOnlyDictionary<DocumentType, string> Text =
        new Dictionary<DocumentType, string>
        {
            [DocumentType.Scp033] = "<size=15>事案012-5-033\nSCP-012 が SCP-033 の影響を受けていることが確認された。\n- Dr. Redheart</size>",
            [DocumentType.Scp096] = "<size=15>事案096-777-A\n観察中、SCP-096 は予兆なく激昂した。現行プロトコルの再評価を要請する。\n- Dr. Redheart</size>",
            [DocumentType.Scp3005] = "<size=15>SCP-3005 の Site-02 への移管について\n収容コンテナの輸送は予定通り完了する。</size>",
            [DocumentType.Backrooms] = "<b><size=55>何故あなたはここにいる？</size></b>",
            [DocumentType.Cafeteria] = "<size=15><b>補充申請書</b>\n申請は受理されました。</size>",
            [DocumentType.DeltaWarhead] = "<size=15>DELTA WARHEAD\n[このファイルの内容は削除されています]</size>",
            [DocumentType.OmegaWarhead] = "<size=15>OMEGA WARHEAD 取扱説明書\n制御室の承認ボタンを操作すること。</size>",
            [DocumentType.ScientistSamuels] = "<size=15>個人アクセスコード: <b>1979</b>\n- 警備主任</size>",
            [DocumentType.ScientistMasoi] = "<size=15>個人アクセスコード: <b>1217</b>\n- 警備主任</size>",
            [DocumentType.ScientistGalia] = "<size=15>個人アクセスコード: <b>1236</b>\n- 警備主任</size>",
            [DocumentType.ScientistJlolldo] = "<size=15>個人アクセスコード: <b>REDACTED</b>\n- 警備主任</size>",
            [DocumentType.AboutSergey] = "<size=15>セルゲイ・マカロフ施設管理官について\n[倫理委員会により削除]</size>",
            [DocumentType.AntiAntiMeme] = "<size=20>Project | Anti: Anti-Meme\nProject Leader | Dr. Maynard</size>",
            [DocumentType.AprilWtf] = "REDACTED",
            [DocumentType.Overbeyond] = "<size=22>シュバルツシルト・クエィサァー</size>\n<size=15>疑似ブラックホールを生む装置だ。</size>",
            [DocumentType.AboutSQ] = "<size=24>If you want to over the beyond, try this.</size>\n<size=30><b>0 7 2 7</b></size>",
            [DocumentType.Kai] = "<size=23>Kai was fucked up by Tentacle :trollface:</size>",
            [DocumentType.OperationDoc] = "<size=18>作戦概要:\nSite-02 のオブジェクトを回収せよ。\n読後は焼却すること。</size>",
            [DocumentType.SpecDoc] = "<size=18>特別作戦指揮書\n最優先事項: 対象の確保と危険人物の排除。</size>",
        };

    public static string Get(DocumentType type)
        => Text.TryGetValue(type, out string value) ? value : string.Empty;
}
