--  PyStudio — Script de Base de Datos MySQL

CREATE DATABASE IF NOT EXISTS pystudio
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE pystudio;

-- 1. USER
--    Todos los usuarios del sistema (estudiantes y profesores).
--    El rol diferencia los permisos en la capa de aplicación.
CREATE TABLE IF NOT EXISTS user (
    id            INT          NOT NULL AUTO_INCREMENT,
    email         VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,           
    full_name     VARCHAR(150) NOT NULL,
    role          ENUM('student','teacher') NOT NULL,
    is_active     BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id),
    UNIQUE  KEY uq_user_email (email)
) ENGINE=InnoDB;

-- 2. COURSE
--    Curso creado por un profesor.
--    access_code es único y generado automáticamente por el backend.
CREATE TABLE IF NOT EXISTS course (
    id            INT          NOT NULL AUTO_INCREMENT,
    teacher_id    INT          NOT NULL,
    name          VARCHAR(150) NOT NULL,
    description   TEXT,
    access_code   VARCHAR(20)  NOT NULL,
    created_at    TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id),
    UNIQUE  KEY uq_course_access_code (access_code),
    CONSTRAINT fk_course_teacher
        FOREIGN KEY (teacher_id) REFERENCES user (id)
        ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE=InnoDB;

-- 3. ENROLLMENT
--    Inscripción de un estudiante en un curso.
--    UNIQUE(student_id, course_id) impide inscripciones duplicadas.
CREATE TABLE IF NOT EXISTS enrollment (
    id          INT       NOT NULL AUTO_INCREMENT,
    student_id  INT       NOT NULL,
    course_id   INT       NOT NULL,
    enrolled_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id),
    UNIQUE  KEY uq_enrollment (student_id, course_id),
    CONSTRAINT fk_enrollment_student
        FOREIGN KEY (student_id) REFERENCES user (id)
        ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT fk_enrollment_course
        FOREIGN KEY (course_id) REFERENCES course (id)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB;

-- 4. ACTIVITY
--    Actividad académica publicada por el profesor dentro de un curso.
CREATE TABLE IF NOT EXISTS activity (
    id          INT          NOT NULL AUTO_INCREMENT,
    course_id   INT          NOT NULL,
    title       VARCHAR(200) NOT NULL,
    description TEXT,
    deadline    TIMESTAMP    NOT NULL,
    created_at  TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id),
    CONSTRAINT fk_activity_course
        FOREIGN KEY (course_id) REFERENCES course (id)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB;

-- 5. ACTIVITY_FILE
--    Archivos adjuntos opcionales de una actividad (plantillas, PDFs…).
CREATE TABLE IF NOT EXISTS activity_file (
    id          INT          NOT NULL AUTO_INCREMENT,
    activity_id INT          NOT NULL,
    file_name   VARCHAR(255) NOT NULL,
    file_path   VARCHAR(500) NOT NULL,

    PRIMARY KEY (id),
    CONSTRAINT fk_activity_file_activity
        FOREIGN KEY (activity_id) REFERENCES activity (id)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB;

-- 6. STUDENT_GROUP
--    Grupo de trabajo formado por estudiantes dentro de un curso.
--    invite_code es único y generado automáticamente por el backend.
CREATE TABLE IF NOT EXISTS student_group (
    id          INT         NOT NULL AUTO_INCREMENT,
    course_id   INT         NOT NULL,
    name        VARCHAR(100) NOT NULL,
    invite_code VARCHAR(20)  NOT NULL,
    created_at  TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id),
    UNIQUE  KEY uq_group_invite_code (invite_code),
    CONSTRAINT fk_student_group_course
        FOREIGN KEY (course_id) REFERENCES course (id)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB;

-- 7. GROUP_MEMBERSHIP
--    Pertenencia de un estudiante a un grupo.
--    UNIQUE(student_id, group_id) evita que un estudiante pueda matricularse en dos grupos distintos de un mismo curso.
CREATE TABLE IF NOT EXISTS group_membership (
    id         INT       NOT NULL AUTO_INCREMENT,
    group_id   INT       NOT NULL,
    student_id INT       NOT NULL,
    joined_at  TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    PRIMARY KEY (id),
    UNIQUE  KEY uq_group_membership (student_id, group_id),
    CONSTRAINT fk_membership_group
        FOREIGN KEY (group_id) REFERENCES student_group (id)
        ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT fk_membership_student
        FOREIGN KEY (student_id) REFERENCES user (id)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB;

-- 8. SUBMISSION
--    Registro de entrega de una actividad (individual o grupal).
--    Exactamente uno de student_id / group_id debe tener valor.
--    signature_valid refleja el estado de la firma digital (RF-18/19).
CREATE TABLE IF NOT EXISTS submission (
    id                  INT       NOT NULL AUTO_INCREMENT,
    activity_id         INT       NOT NULL,
    student_id          INT,                           -- NULL si es grupal
    group_id            INT,                           -- NULL si es individual
    is_group_submission BOOLEAN   NOT NULL DEFAULT FALSE,
    submitted_at        TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    signature_valid     BOOLEAN   NOT NULL DEFAULT TRUE,

    PRIMARY KEY (id),
    CONSTRAINT fk_submission_activity
        FOREIGN KEY (activity_id) REFERENCES activity (id)
        ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT fk_submission_student
        FOREIGN KEY (student_id) REFERENCES user (id)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_submission_group
        FOREIGN KEY (group_id) REFERENCES student_group (id)
        ON UPDATE CASCADE ON DELETE RESTRICT,
    -- Garantiza que no existan dos entregas individuales del mismo estudiante
    -- para la misma actividad
    CONSTRAINT chk_submission_owner
        CHECK (
            (student_id IS NOT NULL AND group_id IS NULL AND is_group_submission = FALSE)
            OR
            (group_id IS NOT NULL AND student_id IS NULL AND is_group_submission = TRUE)
        )
) ENGINE=InnoDB;

-- 9. SUBMISSION_FILE
--    Archivos .py individuales de una entrega.
--    Cada archivo tiene su propia firma digital.
CREATE TABLE IF NOT EXISTS submission_file (
    id                INT          NOT NULL AUTO_INCREMENT,
    submission_id     INT          NOT NULL,
    file_name         VARCHAR(255) NOT NULL,
    file_path         VARCHAR(500) NOT NULL,
    digital_signature TEXT,                   -- NULL si la firma falló 

    PRIMARY KEY (id),
    CONSTRAINT fk_sub_file_submission
        FOREIGN KEY (submission_id) REFERENCES submission (id)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB;

-- 10. GIT_COMMIT
--     Commit automático generado por el IDE (RF-20).
--     Permite al profesor ver la evolución del código.
CREATE TABLE IF NOT EXISTS git_commit (
    id           INT          NOT NULL AUTO_INCREMENT,
    submission_id INT         NOT NULL,
    commit_hash  VARCHAR(64)  NOT NULL,   
    message      VARCHAR(255) NOT NULL,  
    committed_at TIMESTAMP    NOT NULL,

    PRIMARY KEY (id),
    CONSTRAINT fk_git_commit_submission
        FOREIGN KEY (submission_id) REFERENCES submission (id)
        ON UPDATE CASCADE ON DELETE CASCADE
) ENGINE=InnoDB;
