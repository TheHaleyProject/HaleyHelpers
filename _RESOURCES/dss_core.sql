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


-- Dumping database structure for dss_core
CREATE DATABASE IF NOT EXISTS `dss_core` /*!40100 DEFAULT CHARACTER SET latin1 COLLATE latin1_swedish_ci */;
USE `dss_core`;

-- Dumping structure for table dss_core.client
CREATE TABLE IF NOT EXISTS `client` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `managed_name` varchar(140) NOT NULL,
  `path` varchar(140) NOT NULL,
  `created_on` datetime NOT NULL DEFAULT current_timestamp(),
  `notes` varchar(600) DEFAULT NULL,
  `guid` varchar(48) NOT NULL DEFAULT uuid() COMMENT 'this guid is used to create separate schema for all the clients where in the files will be stored.',
  `fullname` varchar(240) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `unq_client` (`name`),
  UNIQUE KEY `unq_client_0` (`managed_name`),
  UNIQUE KEY `unq_client_1` (`guid`)
) ENGINE=InnoDB AUTO_INCREMENT=1000 DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_core.client_suffix
CREATE TABLE IF NOT EXISTS `client_suffix` (
  `id` int(11) NOT NULL,
  `file` varchar(1) NOT NULL DEFAULT 'f',
  `directory` varchar(1) NOT NULL DEFAULT 'd',
  PRIMARY KEY (`id`),
  CONSTRAINT `fk_client_suffix_client` FOREIGN KEY (`id`) REFERENCES `client` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_core.directory
CREATE TABLE IF NOT EXISTS `directory` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `name` varchar(120) NOT NULL,
  `managed_name` varchar(140) NOT NULL,
  `path` varchar(140) DEFAULT NULL COMMENT 'Path is not mandatory. Sometimes if it is not present, we just assume that the data is present in the base directory level. As file ids are created at client level (and not at directory level), not having a path will not create any conflicts. Only file ids will not create issues. However, storing the file itself might create conflicts. So need to be careful with that approach.',
  `created_on` datetime NOT NULL DEFAULT current_timestamp(),
  `notes` varchar(600) DEFAULT NULL,
  `parent` int(11) NOT NULL,
  `type` int(11) NOT NULL,
  `isactive` bit(1) NOT NULL DEFAULT b'1',
  `guid` varchar(48) NOT NULL DEFAULT 'uuid()',
  PRIMARY KEY (`id`),
  UNIQUE KEY `unq_directory` (`name`),
  UNIQUE KEY `unq_directory_0` (`managed_name`),
  UNIQUE KEY `unq_directory_1` (`guid`),
  KEY `idx_directory` (`parent`,`name`),
  KEY `idx_directory_0` (`parent`,`managed_name`),
  KEY `idx_directory_1` (`type`),
  KEY `idx_directory_2` (`parent`,`type`),
  CONSTRAINT `fk_direcory_client` FOREIGN KEY (`parent`) REFERENCES `client` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `fk_directory_tag` FOREIGN KEY (`type`) REFERENCES `tag` (`id`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB AUTO_INCREMENT=1000 DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

-- Data exporting was unselected.

-- Dumping structure for table dss_core.tag
CREATE TABLE IF NOT EXISTS `tag` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `unq_tag` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

-- Data exporting was unselected.

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
