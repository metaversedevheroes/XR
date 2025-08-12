using UnityEngine;

public struct HitInfo {
    public AttackContext context;   // 누가 누구를 어떤 포인트에서 떄렸나
    public DamageResult  result;    // 때린 결과가 어떻게 되었나
    // 만약 원한다면 공격할 때, 맞을 때의 이펙트 애니메이션, 소리 관련된 거 여기에 데이터로 만들어도 될듣ㅅ??
}

