-- --------------------------------------------------------
-- Host:                         127.0.0.1
-- Server version:               11.7.2-MariaDB - mariadb.org binary distribution
-- Server OS:                    Win64
-- HeidiSQL Version:             12.7.0.6850
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


-- Dumping database structure for dss_client
CREATE DATABASE IF NOT EXISTS `dss_client` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_uca1400_ai_ci */;
USE `dss_client`;

-- Dumping structure for table dss_client.directory
CREATE TABLE IF NOT EXISTS `directory` (
  `module` bigint(20) NOT NULL,
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `parent` bigint(20) NOT NULL DEFAULT 0 COMMENT 'For a root this is nullable (we just fill with zero to denote that this is root directory). Don''t reference with a foreign key.',
  `stored_name` varchar(300) NOT NULL,
  `created` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `unq_directory` (`module`,`parent`,`stored_name`),
  CONSTRAINT `fk_directory_module` FOREIGN KEY (`module`) REFERENCES `module` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_client.directory_info
CREATE TABLE IF NOT EXISTS `directory_info` (
  `id` bigint(20) NOT NULL,
  `name` varchar(240) NOT NULL,
  `display_name` varchar(240) NOT NULL,
  `path` text DEFAULT NULL COMMENT 'cached full path for performance',
  `valid` bit(1) NOT NULL DEFAULT b'0' COMMENT 'Is Path valid',
  PRIMARY KEY (`id`),
  CONSTRAINT `fk_directory_info_directory` FOREIGN KEY (`id`) REFERENCES `directory` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_client.document
CREATE TABLE IF NOT EXISTS `document` (
  `parent` bigint(20) NOT NULL,
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `guid` varchar(48) NOT NULL DEFAULT 'uuid()' COMMENT 'Its unique GUID, not generated from the hash',
  `created` timestamp NOT NULL DEFAULT current_timestamp(),
  `modified` timestamp NOT NULL DEFAULT current_timestamp(),
  `stored_name` varchar(240) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `unq_file_index` (`guid`),
  UNIQUE KEY `unq_document` (`parent`,`stored_name`),
  KEY `fk_file_index_parent` (`parent`),
  CONSTRAINT `fk_document_directory` FOREIGN KEY (`parent`) REFERENCES `directory` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_client.doc_info
CREATE TABLE IF NOT EXISTS `doc_info` (
  `extension` int(11) DEFAULT NULL,
  `file` bigint(20) NOT NULL,
  `name` varchar(400) NOT NULL,
  `display_name` varchar(240) NOT NULL,
  `path` text DEFAULT NULL COMMENT 'cached for performance',
  `valid` int(11) DEFAULT NULL,
  PRIMARY KEY (`file`),
  KEY `idx_doc_info_name` (`name`),
  KEY `idx_doc_info` (`extension`,`name`),
  CONSTRAINT `fk_doc_info_extension` FOREIGN KEY (`extension`) REFERENCES `extension` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fk_file_info_file_index_0` FOREIGN KEY (`file`) REFERENCES `document` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_client.doc_version
CREATE TABLE IF NOT EXISTS `doc_version` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `doc` bigint(20) NOT NULL,
  `created` timestamp NOT NULL DEFAULT current_timestamp(),
  `size` bigint(20) NOT NULL COMMENT 'in bytes',
  `version` int(11) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  KEY `idx_file_version` (`doc`,`version`),
  KEY `idx_file_version_0` (`created`),
  CONSTRAINT `fk_file_version_file_index` FOREIGN KEY (`doc`) REFERENCES `document` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_client.extension
CREATE TABLE IF NOT EXISTS `extension` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_client.module
CREATE TABLE IF NOT EXISTS `module` (
  `id` bigint(20) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Data exporting was unselected.

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
