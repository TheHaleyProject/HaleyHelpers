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
  `id` bigint(20) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_client.extensions
CREATE TABLE IF NOT EXISTS `extensions` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_client.file_extension
CREATE TABLE IF NOT EXISTS `file_extension` (
  `file` bigint(20) NOT NULL,
  `extension` int(11) NOT NULL,
  PRIMARY KEY (`file`),
  KEY `fk_file_extension_extensions` (`extension`),
  CONSTRAINT `fk_file_extension_extensions` FOREIGN KEY (`extension`) REFERENCES `extensions` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fk_file_extension_file_index` FOREIGN KEY (`file`) REFERENCES `file_index` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_client.file_index
CREATE TABLE IF NOT EXISTS `file_index` (
  `parent` bigint(20) NOT NULL,
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `guid` varchar(48) NOT NULL DEFAULT 'uuid()',
  PRIMARY KEY (`id`),
  UNIQUE KEY `unq_file_index` (`guid`),
  KEY `fk_file_index_parent` (`parent`),
  CONSTRAINT `fk_file_index_parent` FOREIGN KEY (`parent`) REFERENCES `directory` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=1500 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_client.file_name
CREATE TABLE IF NOT EXISTS `file_name` (
  `file` bigint(20) NOT NULL,
  `managed_name` varchar(300) NOT NULL,
  `original_name` varchar(400) NOT NULL,
  PRIMARY KEY (`file`),
  KEY `idx_file_name` (`original_name`),
  KEY `idx_file_name_0` (`managed_name`),
  CONSTRAINT `fk_file_info_file_index_0` FOREIGN KEY (`file`) REFERENCES `file_index` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_client.file_path
CREATE TABLE IF NOT EXISTS `file_path` (
  `version` bigint(20) NOT NULL,
  `parent` bigint(20) NOT NULL,
  `path` text NOT NULL,
  PRIMARY KEY (`version`),
  UNIQUE KEY `unq_file_path` (`parent`,`path`) USING HASH,
  KEY `fk_file_path_parent` (`parent`),
  CONSTRAINT `fk_file_path_file_version` FOREIGN KEY (`version`) REFERENCES `file_version` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fk_file_path_parent` FOREIGN KEY (`parent`) REFERENCES `directory` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_client.file_version
CREATE TABLE IF NOT EXISTS `file_version` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `file` bigint(20) NOT NULL,
  `uploaded_time` datetime NOT NULL DEFAULT current_timestamp(),
  `size` bigint(20) NOT NULL COMMENT 'in bytes',
  `version` int(11) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  KEY `idx_file_version` (`file`,`version`),
  KEY `idx_file_version_0` (`uploaded_time`),
  CONSTRAINT `fk_file_version_file_index` FOREIGN KEY (`file`) REFERENCES `file_index` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=250 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Data exporting was unselected.

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
