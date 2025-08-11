using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

public class HeadDirectionMovement : MonoBehaviour
{
    [Header("References")]
    public Transform cameraTransform;

    [Header("Settings")]
    public float moveSpeed = 1.5f;

    private XRHandSubsystem handSubsystem;

    void Start()
    {
        // XRHandSubsystem 가져오기
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
            handSubsystem = subsystems[0];
    }

    void Update()
    {
        if (handSubsystem == null) return;

        var left = handSubsystem.leftHand;
        var right = handSubsystem.rightHand;

        // 손 추적 여부 + 내부 데이터 유효성 체크
        if (!IsHandUsable(left) || !IsHandUsable(right)) return;

        // 양손 모두 주먹 쥐고 있다면 이동
        if (IsFist(left) && IsFist(right))
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            transform.position += forward * moveSpeed * Time.deltaTime;

        }
    }

    bool IsFist(XRHand hand)
    {
        return IsFingerBent(hand, XRHandJointID.IndexTip, XRHandJointID.IndexProximal) &&
               IsFingerBent(hand, XRHandJointID.MiddleTip, XRHandJointID.MiddleProximal) &&
               IsFingerBent(hand, XRHandJointID.RingTip, XRHandJointID.RingProximal) &&
               IsFingerBent(hand, XRHandJointID.LittleTip, XRHandJointID.LittleProximal);
    }

    bool IsFingerBent(XRHand hand, XRHandJointID tip, XRHandJointID baseJoint)
    {
        if (!hand.GetJoint(tip).TryGetPose(out Pose tipPose)) return false;
        if (!hand.GetJoint(baseJoint).TryGetPose(out Pose basePose)) return false;

        float distance = Vector3.Distance(tipPose.position, basePose.position);
        return distance < 0.04f;
    }

    bool IsHandUsable(XRHand hand)
    {
        if (!hand.isTracked) return false;
        try
        {
            // palm 관절이 접근 가능한지 확인
            return hand.GetJoint(XRHandJointID.Palm).TryGetPose(out _);
        }
        catch
        {
            return false;
        }
    }
}
