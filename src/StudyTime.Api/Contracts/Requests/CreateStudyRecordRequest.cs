using System.ComponentModel.DataAnnotations;

namespace StudyTime.Api.Contracts.Requests;

public sealed record CreateStudyRecordRequest(
    [param: Range(1, int.MaxValue)]
    int Minutes);
