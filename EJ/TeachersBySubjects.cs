//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace EJ
{
    using System;
    using System.Collections.Generic;
    
    public partial class TeachersBySubjects
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public int SubjectId { get; set; }
    
        public virtual Subjects Subjects { get; set; }
        public virtual Teachers Teachers { get; set; }
    }
}
